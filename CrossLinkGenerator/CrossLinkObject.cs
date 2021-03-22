// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

        // Link target
        Linked = 1 << 21, // LinkType != LinkType.None
        AutoLink = 1 << 22,
        AutoNotify = 1 << 23,
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

        public string LinkName { get; private set; } = string.Empty;

        public string ChainName { get; private set; } = string.Empty;

        public HashSet<string> UsedKeywords { get; private set; } = new();

        public CrossLinkObject[] Members { get; private set; } = Array.Empty<CrossLinkObject>(); // Members have valid TypeObject && not static && property or field

        public IEnumerable<CrossLinkObject> MembersWithFlag(CrossLinkObjectFlag flag) => this.Members.Where(x => x.ObjectFlag.HasFlag(flag));

        public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

        public List<CrossLinkObject>? Children { get; private set; } // The opposite of ContainingObject

        public List<CrossLinkObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

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
            var genericsType = this.Generics_Kind;
            if (genericsType == VisceralGenericsKind.OpenGeneric)
            {
                return;
            }

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

            // LinkAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == LinkAttributeMock.FullName) is { } linkAttribute)
            {
                try
                {
                    this.LinkAttribute = LinkAttributeMock.FromArray(linkAttribute.ConstructorArguments, linkAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(CrossLinkBody.Error_AttributePropertyError, linkAttribute.Location);
                }
            }

            if (this.LinkAttribute != null)
            {
                if (this.LinkAttribute.Type != LinkType.None)
                {// Valid link type
                    this.ObjectFlag |= CrossLinkObjectFlag.Linked;
                    if (this.LinkAttribute.AutoLink)
                    {
                        this.ObjectFlag |= CrossLinkObjectFlag.AutoLink;
                    }
                }
                else
                {// No link
                }

                if (this.LinkAttribute.AutoNotify)
                {
                    this.ObjectFlag |= CrossLinkObjectFlag.AutoNotify;
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
            foreach (var x in this.AllMembers)
            {
                this.UsedKeywords.Add(x.SimpleName);
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

            this.Members = list.Where(x => x.LinkAttribute != null).ToArray();

            foreach (var x in this.Members)
            {
                if (x.LinkAttribute!.Type != LinkType.None)
                {
                    this.ObjectFlag |= CrossLinkObjectFlag.HasLink;
                }

                if (x.LinkAttribute!.AutoNotify)
                {
                    this.ObjectFlag |= CrossLinkObjectFlag.HasNotify;
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

            // Check members.
            foreach (var x in this.Members)
            {
                x.CheckMember(this);
                if (x.LinkAttribute != null)
                {
                    this.CheckKeyword(x.LinkAttribute.Name, x.Location);
                    if (x.LinkAttribute.Type != LinkType.None)
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

                if (this.LinkAttribute.Type != LinkType.None)
                {// Valid link
                    if (this.LinkAttribute.Name == string.Empty)
                    {
                        var name = this.SimpleName.ToCharArray();
                        if (!char.IsLower(name[0]))
                        {// Link name must start with a lowercase letter.
                            this.Body.AddDiagnostic(CrossLinkBody.Error_LinkTargetNameError, this.Location, this.SimpleName);
                        }
                        else
                        {
                            name[0] = char.ToUpperInvariant(name[0]);
                            this.LinkAttribute.Name = new string(name);
                        }
                    }

                    this.LinkName = this.LinkAttribute.Name + "Link";
                    this.ChainName = this.LinkAttribute.Name + "Chain";
                }
            }
        }

        public void CheckKeyword(string keyword, Location? location = null)
        {
            if (this.UsedKeywords.Contains(keyword))
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
                foreach (var x in this.ConstructedObjects)
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
                }

                // StringKey fields
                // this.GenerateStringKeyFields(ssb, info);

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

            foreach (var x in this.Members)
            {
                this.GenerateLink(ssb, info, x);
            }

            return;
        }

        internal void GenerateLink(ScopingStringBuilder ssb, GeneratorInformation info, CrossLinkObject x)
        {
            this.GenerateLink_Property(ssb, info, x);
            this.GenerateLink_Link(ssb, info, x);
        }

        internal void GenerateLink_Property(ScopingStringBuilder ssb, GeneratorInformation info, CrossLinkObject x)
        {
            var link = x.LinkAttribute;
            if (x.TypeObject == null || link == null)
            {
                return;
            }

            using (var scopeProperty = ssb.ScopeBrace($"public {x.TypeObject.FullName} {link.Name}"))
            {
                ssb.AppendLine($"get => this.{x.SimpleName};");
                using (var scopeSet = ssb.ScopeBrace("set"))
                {
                    string compare;
                    if (x.IsPrimitive)
                    {
                        compare = $"value != this.{x.SimpleName}";
                    }
                    else
                    {
                        compare = $"!EqualityComparer<{x.TypeObject.FullName}>.Default.Equals(value, this.{x.SimpleName})";
                    }

                    using (var scopeCompare = ssb.ScopeBrace(compare))
                    {
                        ssb.AppendLine($"this.{x.SimpleName} = value;");
                        if (link.AutoLink)
                        {
                            ssb.AppendLine($"this.{goshujinInstance}.Remove(this);");
                        }
                    }
                }
            }
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
                foreach (var chain in this.Members)
                {
                    var link = chain.LinkAttribute!;
                    if (!link.AutoLink || link.Type == LinkType.None)
                    {// Manual link or invalid link
                        continue;
                    }
                    else if (link.Type == LinkType.Ordered)
                    {
                        ssb.AppendLine($"this.{chain.ChainName}.Add({ssb.FullObject}, {ssb.FullObject}.{chain.SimpleName});");
                    }
                    else if (link.Type == LinkType.LinkedList)
                    {
                        ssb.AppendLine($"this.{chain.ChainName}.AddLast({ssb.FullObject});");
                    }
                    else if (link.Type == LinkType.StackList)
                    {
                        ssb.AppendLine($"this.{chain.ChainName}.Push({ssb.FullObject});");
                    }
                    else
                    {
                        ssb.AppendLine($"this.{chain.ChainName}.Add({ssb.FullObject});");
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
                foreach (var chain in this.Members)
                {
                    var link = chain.LinkAttribute!;
                    if (link.Type == LinkType.None)
                    {
                        continue;
                    }
                    else
                    {
                        ssb.AppendLine($"this.{chain.ChainName}.Remove({ssb.FullObject});");
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Chain(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            foreach (var chain in this.Members)
            {
                var link = chain.LinkAttribute!;
                if (link.Type == LinkType.None)
                {
                    continue;
                }
                else if (link.Type == LinkType.Ordered)
                {
                    ssb.AppendLine($"public {link.Type.LinkTypeToChain()}<{chain.TypeObject!.FullName}, {this.LocalName}> {chain.ChainName} {{ get; }} = new(static x => ref x.{chain.LinkName});");
                }
                else
                {
                    ssb.AppendLine($"public {link.Type.LinkTypeToChain()}<{this.LocalName}> {chain.ChainName} {{ get; }} = new(static x => ref x.{chain.LinkName});");
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
