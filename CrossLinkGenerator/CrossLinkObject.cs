﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1602 // Enumeration items should be documented

namespace CrossLink.Generator
{
    public enum MethodCondition
    {
        MemberMethod, // member method (generated)
        StaticMethod, // static method (generated, for generic class)
        Declared, // declared (user-declared)
        ExplicitlyDeclared, // declared (explicit interface)
    }

    [Flags]
    public enum CrossLinkObjectFlag
    {
        Configured = 1 << 0,
        RelationConfigured = 1 << 1,
        Checked = 1 << 2,

        // Link object
        HasLink = 1 << 10, // Has valid link (not LinkType.None)
        HasNotify = 1 << 11, // Has AutoNotify
        CanCreateInstance = 1 << 12, // Can create an instance
    }

    public class CrossLinkObject : VisceralObjectBase<CrossLinkObject>
    {
        public CrossLinkObject()
        {
        }

        public new CrossLinkBody Body => (CrossLinkBody)((VisceralObjectBase<CrossLinkObject>)this).Body;

        public CrossLinkObjectFlag ObjectFlag { get; private set; }

        public CrossLinkObjectAttributeMock? ObjectAttribute { get; private set; }

        public LinkAttributeMock? LinkAttribute { get; private set; }

        public List<Linkage>? Links { get; private set; } = null;

        public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

        public List<CrossLinkObject>? Children { get; private set; } // The opposite of ContainingObject

        public List<CrossLinkObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

        public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

        public int GenericsNumber { get; private set; }

        public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

        public Arc.Visceral.NullableAnnotation NullableAnnotationIfReferenceType
        {
            get
            {
                if (this.TypeObject?.Kind.IsReferenceType() == true)
                {
                    if (this.symbol is IFieldSymbol fs)
                    {
                        return (Arc.Visceral.NullableAnnotation)fs.NullableAnnotation;
                    }
                    else if (this.symbol is IPropertySymbol ps)
                    {
                        return (Arc.Visceral.NullableAnnotation)ps.NullableAnnotation;
                    }
                }

                return Arc.Visceral.NullableAnnotation.None;
            }
        }

        public string QuestionMarkIfReferenceType
        {
            get
            {
                if (this.Kind.IsReferenceType())
                {
                    return "?";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public void Configure()
        {
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.Configured))
            {
                return;
            }

            this.ObjectFlag |= CrossLinkObjectFlag.Configured;

            // Open generic type is not supported.
            /*var genericsType = this.Generics_Kind;
            if (genericsType == VisceralGenericsKind.OpenGeneric)
            {
                return;
            }*/

            // CrossLinkObjectAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == CrossLinkObjectAttributeMock.FullName) is { } objectAttribute)
            {
                this.Location = objectAttribute.Location;
                try
                {
                    this.ObjectAttribute = CrossLinkObjectAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);

                    // Goshujin Class / Instance
                    this.ObjectAttribute.GoshujinClass = (this.ObjectAttribute.GoshujinClass != string.Empty) ? this.ObjectAttribute.GoshujinClass : CrossLinkBody.DefaultGoshujinClass;
                    this.ObjectAttribute.GoshujinInstance = (this.ObjectAttribute.GoshujinInstance != string.Empty) ? this.ObjectAttribute.GoshujinInstance : CrossLinkBody.DefaultGoshujinInstance;
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(CrossLinkBody.Error_AttributePropertyError, objectAttribute.Location);
                }
            }

            // Linkage
            bool linkageFlag = false;
            foreach (var linkAttribute in this.AllAttributes.Where(x => x.FullName == LinkAttributeMock.FullName))
            {
                if (linkageFlag && !this.Method_IsConstructor)
                {// One link is allowed per member.
                    this.Body.AddDiagnostic(CrossLinkBody.Error_MultipleLink, linkAttribute.Location);
                }

                linkageFlag = true;
                var linkage = Linkage.Create(this, linkAttribute);
                if (linkage == null)
                {
                    continue;
                }

                if (this.ContainingObject is { } parent)
                {// Add to parent's list
                    if (parent.Links == null)
                    {
                        parent.Links = new();
                    }

                    parent.Links.Add(linkage);
                }
            }

            if (this.ObjectAttribute != null)
            {// CrossLinkObject
                this.ConfigureObject();
            }
        }

        private void ConfigureObject()
        {
            // Used keywords
            this.Identifier = new VisceralIdentifier();
            foreach (var x in this.AllMembers)
            {
                this.Identifier.Add(x.SimpleName);
            }

            // Members: Property
            var list = new List<CrossLinkObject>();
            foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Property))
            {
                if (x.TypeObject != null && !x.IsStatic)
                { // Valid TypeObject && not static
                    x.Configure();
                    list.Add(x);
                }
            }

            // Members: Field
            foreach (var x in this.AllMembers.Where(x => x.Kind == VisceralObjectKind.Field))
            {
                if (x.TypeObject != null && !x.IsStatic)
                { // Valid TypeObject && not static
                    x.Configure();
                    list.Add(x);
                }
            }

            if (this.Links != null)
            {
                foreach (var x in this.Links)
                {
                    if (x.IsValidLink)
                    {
                        this.ObjectFlag |= CrossLinkObjectFlag.HasLink;
                    }

                    if (x.AutoNotify)
                    {
                        this.ObjectFlag |= CrossLinkObjectFlag.HasNotify;
                    }
                }
            }
        }

        public void ConfigureRelation()
        {// Create an object tree.
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.RelationConfigured))
            {
                return;
            }

            this.ObjectFlag |= CrossLinkObjectFlag.RelationConfigured;

            if (!this.Kind.IsType())
            {// Not type
                return;
            }

            var cf = this.OriginalDefinition;
            if (cf == null)
            {
                return;
            }
            else if (cf != this)
            {
                cf.ConfigureRelation();
            }

            if (cf.ContainingObject == null)
            {// Root object
                List<CrossLinkObject>? list;
                if (!this.Body.Namespaces.TryGetValue(this.Namespace, out list))
                {// Create a new namespace.
                    list = new();
                    this.Body.Namespaces[this.Namespace] = list;
                }

                if (!list.Contains(cf))
                {
                    list.Add(cf);
                }
            }
            else
            {// Child object
                var parent = cf.ContainingObject;
                parent.ConfigureRelation();
                if (parent.Children == null)
                {
                    parent.Children = new();
                }

                if (!parent.Children.Contains(cf))
                {
                    parent.Children.Add(cf);
                }
            }

            if (this.Generics_Kind != VisceralGenericsKind.OpenGeneric)
            {
                if (cf.ConstructedObjects == null)
                {
                    cf.ConstructedObjects = new();
                }

                if (!cf.ConstructedObjects.Contains(this))
                {
                    cf.ConstructedObjects.Add(this);
                    this.GenericsNumber = cf.ConstructedObjects.Count;
                }
            }
        }

        public void CheckObject()
        {
            if (!this.IsAbstractOrInterface)
            {
                this.ObjectFlag |= CrossLinkObjectFlag.CanCreateInstance;
            }

            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.CanCreateInstance))
            {// Type which can create an instance
                // partial class required.
                if (!this.IsPartial)
                {
                    this.Body.ReportDiagnostic(CrossLinkBody.Error_NotPartial, this.Location, this.FullName);
                }

                // Parent class also needs to be a partial class.
                var parent = this.ContainingObject;
                while (parent != null)
                {
                    if (!parent.IsPartial)
                    {
                        this.Body.ReportDiagnostic(CrossLinkBody.Error_NotPartialParent, parent.Location, parent.FullName);
                    }

                    parent = parent.ContainingObject;
                }
            }

            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.HasLink))
            {// Check Goshujin Class / Instance
                this.CheckKeyword(this.ObjectAttribute!.GoshujinClass, this.Location);
                this.CheckKeyword(this.ObjectAttribute!.GoshujinInstance, this.Location);
            }

            // Check Links.
            if (this.Links != null)
            {
                foreach (var x in this.Links)
                {
                    if (x.Target != null)
                    {
                        x.Target.CheckMember(this);
                    }

                    this.CheckKeyword(x.Name, x.Location);
                    if (x.IsValidLink)
                    {
                        this.CheckKeyword(x.LinkName, x.Location);
                    }
                }
            }
        }

        public void CheckMember(CrossLinkObject parent)
        {
            // Avoid this.TypeObject!
            if (this.TypeObject == null)
            {
                return;
            }

            if (this.LinkAttribute != null)
            {
                /*if (this.Kind != VisceralObjectKind.Field)
                {// Link target must be a field
                    this.Body.AddDiagnostic(CrossLinkBody.Error_LinkTargetNotField, this.Location, this.SimpleName);
                }*/
            }
        }

        public void CheckKeyword(string keyword, Location? location = null)
        {
            var st = this.Identifier.GetIdentifier();
            if (!this.Identifier.Add(keyword))
            {
                this.Body.AddDiagnostic(CrossLinkBody.Error_KeywordUsed, location ?? Location.None, this.SimpleName, keyword);
            }
        }

        public void Check()
        {
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.Checked))
            {
                return;
            }

            this.ObjectFlag |= CrossLinkObjectFlag.Checked;

            this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
            this.CheckObject();
        }

        public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, List<CrossLinkObject> list)
        {
            var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null);

            if (list.Count > 0 && list[0].ContainingObject is { } containingObject)
            {
                // info.ModuleInitializerClass.Add(containingObject.FullName);
                var constructedList = containingObject.ConstructedObjects;
                if (constructedList != null && constructedList.Count > 0)
                {
                    info.ModuleInitializerClass.Add(constructedList[0].FullName);
                }
            }

            /* using (var m = ssb.ScopeBrace("internal static void __gen__load()"))
            {
                foreach (var x in list2)
                {
                }
            }

            ssb.AppendLine();

            foreach (var x in list2)
            {
            }*/
        }

        internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.ConstructedObjects == null)
            {
                return;
            }
            else if (this.IsAbstractOrInterface)
            {
                return;
            }

            var interfaceString = string.Empty;
            if (this.ObjectAttribute != null)
            {
                // interfaceString = " : ITinyhandSerialize";
            }

            using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}{interfaceString}"))
            {
                if (this.ObjectAttribute != null)
                {
                    this.Generate2(ssb, info);
                }

                /*foreach (var x in this.ConstructedObjects)
                {
                    if (x.ObjectAttribute == null)
                    {
                        continue;
                    }

                    if (x.GenericsNumber > 1)
                    {
                        ssb.AppendLine();
                    }

                    x.Generate2(ssb, info);
                }*/

                if (this.Children?.Count > 0)
                {// Generate children and loader.
                    ssb.AppendLine();
                    foreach (var x in this.Children)
                    {
                        x.Generate(ssb, info);
                    }

                    ssb.AppendLine();
                    GenerateLoader(ssb, info, this.Children);
                }
            }
        }

        internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.HasLink))
            {
                this.GenerateGoshujinClass(ssb, info);
                this.GenerateGoshujinInstance(ssb, info);
            }

            if (this.Links != null)
            {
                foreach (var x in this.Links)
                {
                    this.GenerateLink(ssb, info, x);
                }
            }

            return;
        }

        internal void GenerateLink(ScopingStringBuilder ssb, GeneratorInformation info, Linkage x)
        {
            this.GenerateLink_Property(ssb, info, x);
            this.GenerateLink_Link(ssb, info, x);
        }

        internal void GenerateLink_Property(ScopingStringBuilder ssb, GeneratorInformation info, Linkage x)
        {
            var target = x.Target;
            if (target == null || target.TypeObject == null)
            {
                return;
            }

            using (var scopeProperty = ssb.ScopeBrace($"public {target.TypeObject.FullName} {x.Name}"))
            {
                ssb.AppendLine($"get => this.{x.TargetName};");
                using (var scopeSet = ssb.ScopeBrace("set"))
                {
                    string compare;
                    if (target.TypeObject.IsPrimitive)
                    {
                        compare = $"if (value != this.{x.TargetName})";
                    }
                    else
                    {
                        compare = $"if (!EqualityComparer<{target.TypeObject.FullName}>.Default.Equals(value, this.{x.TargetName}))";
                    }

                    using (var scopeCompare = ssb.ScopeBrace(compare))
                    {
                        ssb.AppendLine($"this.{x.TargetName} = value;");
                        if (x.AutoLink)
                        {
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateLink_Link(ScopingStringBuilder ssb, GeneratorInformation info, Linkage x)
        {
            if (x.Type == LinkType.None)
            {
                return;
            }
            else if (x.Type == LinkType.Ordered && x.Target != null && x.Target.TypeObject != null)
            {
                ssb.AppendLine($"public {x.Type.LinkTypeToChain()}<{x.Target!.TypeObject!.FullName}, {this.LocalName}>.Link {x.LinkName};");
            }
            else
            {
                ssb.AppendLine($"public {x.Type.LinkTypeToChain()}<{this.LocalName}>.Link {x.LinkName};");
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujinClass(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            var goshujinClass = this.ObjectAttribute!.GoshujinClass;
            using (var scopeClass = ssb.ScopeBrace("public sealed class " + goshujinClass))
            {
                // Constructor
                ssb.AppendLine("public " + goshujinClass + "() {}");
                ssb.AppendLine();

                this.GenerateGoshujin_Add(ssb, info);
                this.GenerateGoshujin_Remove(ssb, info);
                this.GenerateGoshujin_Chain(ssb, info);
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Add(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var scopeParameter = ssb.ScopeObject("x"))
            using (var scopeMethod = ssb.ScopeBrace($"public void Add({this.LocalName} {ssb.FullObject})"))
            {
                if (this.Links != null)
                {
                    foreach (var link in this.Links)
                    {
                        if (!link.AutoLink || link.Type == LinkType.None)
                        {// Manual link or invalid link
                            continue;
                        }
                        else if (link.Type == LinkType.Ordered)
                        {
                            ssb.AppendLine($"this.{link.ChainName}.Add({ssb.FullObject}, {ssb.FullObject}.{link.Target!.SimpleName});");
                        }
                        else if (link.Type == LinkType.LinkedList)
                        {
                            ssb.AppendLine($"this.{link.ChainName}.AddLast({ssb.FullObject});");
                        }
                        else if (link.Type == LinkType.StackList)
                        {
                            ssb.AppendLine($"this.{link.ChainName}.Push({ssb.FullObject});");
                        }
                        else
                        {
                            ssb.AppendLine($"this.{link.ChainName}.Add({ssb.FullObject});");
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Remove(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var scopeParameter = ssb.ScopeObject("x"))
            using (var scopeMethod = ssb.ScopeBrace($"public void Remove({this.LocalName} {ssb.FullObject})"))
            {
                if (this.Links != null)
                {
                    foreach (var link in this.Links)
                    {
                        if (link.Type == LinkType.None)
                        {
                            continue;
                        }
                        else
                        {
                            ssb.AppendLine($"this.{link.ChainName}.Remove({ssb.FullObject});");
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Chain(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.Links == null)
            {
                return;
            }

            foreach (var link in this.Links)
            {
                if (link.Type == LinkType.None)
                {
                    continue;
                }
                else if (link.Type == LinkType.Ordered)
                {
                    ssb.AppendLine($"public {link.Type.LinkTypeToChain()}<{link.Target!.TypeObject!.FullName}, {this.LocalName}> {link.ChainName} {{ get; }} = new(static x => ref x.{link.LinkName});");
                }
                else
                {
                    ssb.AppendLine($"public {link.Type.LinkTypeToChain()}<{this.LocalName}> {link.ChainName} {{ get; }} = new(static x => ref x.{link.LinkName});");
                }
            }
        }

        internal void GenerateGoshujinInstance(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            var goshujin = this.ObjectAttribute!.GoshujinInstance;
            var goshujinInstance = goshujin + "Instance";

            using (var scopeProperty = ssb.ScopeBrace($"public {this.ObjectAttribute!.GoshujinClass} {goshujin}"))
            {
                ssb.AppendLine($"get => this.{goshujinInstance};");
                using (var scopeSet = ssb.ScopeBrace("set"))
                {
                    using (var scopeIfNull = ssb.ScopeBrace($"if (this.{goshujinInstance} != null)"))
                    {
                        ssb.AppendLine($"this.{goshujinInstance}.Remove(this);");
                    }

                    ssb.AppendLine();
                    ssb.AppendLine($"this.{goshujinInstance} = value;");
                    ssb.AppendLine($"this.{goshujinInstance}.Add(this);");
                }
            }

            ssb.AppendLine();
            ssb.AppendLine($"private {this.ObjectAttribute!.GoshujinClass} {goshujinInstance} = default!;");
            ssb.AppendLine();
        }
    }
}