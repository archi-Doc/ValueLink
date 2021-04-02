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
    public enum DeclarationCondition
    {
        NotDeclared, // Not declared
        ImplicitlyDeclared, // declared (implicitly)
        ExplicitlyDeclared, // declared (explicitly interface)
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
        GenerateINotifyPropertyChanged = 1 << 13, // Generate INotifyPropertyChanged
        TinyhandObject = 1 << 14, // Has TinyhandObjectAttribute
    }

    public class CrossLinkObject : VisceralObjectBase<CrossLinkObject>
    {
        public CrossLinkObject()
        {
        }

        public new CrossLinkBody Body => (CrossLinkBody)((VisceralObjectBase<CrossLinkObject>)this).Body;

        public CrossLinkObjectFlag ObjectFlag { get; private set; }

        public CrossLinkObjectAttributeMock? ObjectAttribute { get; private set; }

        public DeclarationCondition PropertyChangedDeclaration { get; private set; }

        public LinkAttributeMock? LinkAttribute { get; private set; }

        public List<Linkage>? Links { get; private set; } = null;

        public int NumberOfValidLinks { get; private set; }

        public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

        public List<CrossLinkObject>? Children { get; private set; } // The opposite of ContainingObject

        public List<CrossLinkObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

        public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

        public string GoshujinInstanceIdentifier = string.Empty;

        public string GoshujinFullName = string.Empty;

        public string SerializeIndexIdentifier = string.Empty;

        public int GenericsNumber { get; private set; }

        public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

        public int FormatterNumber { get; private set; }

        public int FormatterExtraNumber { get; private set; }

        internal Automata<CrossLinkObject, Linkage>? DeserializeChainAutomata { get; private set; }

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
                    this.ObjectAttribute.ExplicitPropertyChanged = (this.ObjectAttribute.ExplicitPropertyChanged != string.Empty) ? this.ObjectAttribute.ExplicitPropertyChanged : CrossLinkBody.ExplicitPropertyChanged;
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

            // TinyhandObjectAttribute
            if (this.AllAttributes.Any(x => x.FullName == "Tinyhand.TinyhandObjectAttribute"))
            {
                this.ObjectFlag |= CrossLinkObjectFlag.TinyhandObject;
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

            // Members: Property / Field / Constructor
            foreach (var x in this.AllMembers)
            {
                var flag = false;
                if (x.Kind == VisceralObjectKind.Property || x.Kind == VisceralObjectKind.Field)
                {
                    flag = true;
                }
                else if (x.Kind == VisceralObjectKind.Method && x.Method_IsConstructor)
                {
                    flag = true;
                }

                if (flag && x.TypeObject != null && !x.IsStatic)
                { // Valid TypeObject && not static
                    x.Configure();
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

            if (this.AllInterfaces.Any(x => x == "System.ComponentModel.INotifyPropertyChanged"))
            {// INotifyPropertyChanged implemented
                if (this.symbol is INamedTypeSymbol nts && nts.Interfaces.Any(x => x.Name == "INotifyPropertyChanged" && x.ContainingNamespace.ToDisplayString() == "System.ComponentModel"))
                {// INotifyPropertyChanged is directly implemented.
                    this.PropertyChangedDeclaration = DeclarationCondition.ExplicitlyDeclared;
                }
                else
                {// Inherited from the parent's class.
                    this.PropertyChangedDeclaration = DeclarationCondition.NotDeclared; // INotifyPropertyChanged
                    this.Body.AddDiagnostic(CrossLinkBody.Warning_PropertyChanged, this.Location, this.SimpleName);
                }
            }
            else if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.HasNotify))
            {// Generate INotifyPropertyChanged
                this.ObjectFlag |= CrossLinkObjectFlag.GenerateINotifyPropertyChanged;
                this.PropertyChangedDeclaration = DeclarationCondition.ImplicitlyDeclared;
            }

            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.TinyhandObject))
            {// TinyhandObject
                this.SerializeIndexIdentifier = this.Identifier.GetIdentifier();
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
                this.GoshujinInstanceIdentifier = this.Identifier.GetIdentifier();
                this.GoshujinFullName = this.FullName + "." + this.ObjectAttribute!.GoshujinClass;
            }

            // Check Links.
            this.NumberOfValidLinks = 0;
            if (this.Links != null)
            {
                foreach (var x in this.Links)
                {
                    if (x.Target != null)
                    {
                        x.Target.CheckMember(this);
                    }

                    var result = this.CheckKeyword(x.Name, x.Location);
                    if (x.IsValidLink && result)
                    {
                        this.CheckKeyword(x.LinkName, x.Location);
                        this.CheckKeyword(x.ChainName, x.Location);
                        this.NumberOfValidLinks++;
                    }

                    // Check
                    if (x.Target == null)
                    {
                        if (x.Type == LinkType.Ordered)
                        {
                            this.Body.AddDiagnostic(CrossLinkBody.Error_NoLinkTarget, x.Location);
                        }

                        if (x.AutoNotify)
                        {
                            this.Body.AddDiagnostic(CrossLinkBody.Error_NoNotifyTarget, x.Location);
                        }
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

        public bool CheckKeyword(string keyword, Location? location = null)
        {
            if (!this.Identifier.Add(keyword))
            {
                this.Body.AddDiagnostic(CrossLinkBody.Error_KeywordUsed, location ?? Location.None, this.SimpleName, keyword);
                return false;
            }

            return true;
        }

        public void Check()
        {
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.Checked))
            {
                return;
            }

            if (this.Generics_Kind == VisceralGenericsKind.CloseGeneric)
            {// Close generic is not necessary.
                return;
            }

            this.ObjectFlag |= CrossLinkObjectFlag.Checked;

            this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
            this.CheckObject();

            this.PrepareAutomata();
        }

        public void PrepareAutomata()
        {
            if (this.Links == null)
            {
                return;
            }

            this.DeserializeChainAutomata = new(this, GenerateDeserializeChain);
            foreach (var x in this.Links.Where(x => x.IsValidLink))
            {
                var ret = this.DeserializeChainAutomata.AddNode(x.ChainName, x);
                if (ret.keyResized)
                {// Key resized
                    this.Body.AddDiagnostic(CrossLinkBody.Warning_StringKeySizeLimit, x.Location, Automata<CrossLinkObject, Linkage>.MaxStringKeySizeInBytes);
                }

                if (ret.result == AutomataAddNodeResult.KeyCollision)
                {// Key collision
                    this.Body.AddDiagnostic(CrossLinkBody.Error_StringKeyConflict, x.Location);
                    if (ret.node != null && ret.node.Member != null)
                    {
                        this.Body.AddDiagnostic(CrossLinkBody.Error_StringKeyConflict, ret.node.Member.Location);
                    }

                    continue;
                }
                else if (ret.result == AutomataAddNodeResult.NullKey)
                {// Null key
                    this.Body.AddDiagnostic(CrossLinkBody.Error_StringKeyNull, x.Location);
                    continue;
                }
                else if (ret.node == null)
                {
                    continue;
                }

                x.ChainNameUtf8 = ret.node.Utf8Name!;
                x.ChainNameIdentifier = this.Identifier.GetIdentifier();
            }
        }

        public static void GenerateDeserializeChain(CrossLinkObject obj, ScopingStringBuilder ssb, object? info, Linkage link)
        {
            ssb.AppendLine("var len = reader.ReadArrayHeader();");
            ssb.AppendLine($"this.{link.ChainName}.Clear();");
            using (var scopeParameter = ssb.ScopeObject("x"))
            using (var scopeFor = ssb.ScopeBrace("for (var n = 0; n < len; n++)"))
            {
                ssb.AppendLine("var i = reader.ReadInt32();");
                ssb.AppendLine("if (i >= max) throw new IndexOutOfRangeException();");
                ssb.AppendLine("var x = array[i];");
                obj.Generate_AddLink(ssb, (GeneratorInformation)info!, link, "this");
                ssb.AppendLine();
            }
        }

        public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, List<CrossLinkObject> list)
        {
            var classFormat = "__gen__cf__{0:D4}";
            var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectFlag.HasFlag(CrossLinkObjectFlag.TinyhandObject));

            if (list.Count > 0 && list[0].ContainingObject is { } containingObject)
            {
                // info.ModuleInitializerClass.Add(containingObject.FullName);
                var constructedList = containingObject.ConstructedObjects;
                if (constructedList != null && constructedList.Count > 0)
                {
                    info.ModuleInitializerClass.Add(constructedList[0].FullName);
                }
            }

            using (var m = ssb.ScopeBrace("internal static void __gen__cl()"))
            {
                foreach (var x in list2)
                {
                    var name = string.Format(classFormat, x.FormatterNumber);
                    ssb.AppendLine($"GeneratedResolver.Instance.SetFormatter<{x.GoshujinFullName}>(new {name}());");
                    name = string.Format(classFormat, x.FormatterExtraNumber);
                    ssb.AppendLine($"GeneratedResolver.Instance.SetFormatterExtra<{x.GoshujinFullName}>(new {name}());");
                }
            }

            ssb.AppendLine();

            foreach (var x in list2)
            {
                var name = string.Format(classFormat, x.FormatterNumber);
                using (var cls = ssb.ScopeBrace($"class {name}: ITinyhandFormatter<{x.GoshujinFullName}>"))
                {
                    // Serialize
                    using (var s = ssb.ScopeBrace($"public void Serialize(ref TinyhandWriter writer, {x.GoshujinFullName + x.QuestionMarkIfReferenceType} v, TinyhandSerializerOptions options)"))
                    {
                        if (x.Kind.IsReferenceType())
                        {// Reference type
                            ssb.AppendLine($"if (v == null) {{ writer.WriteNil(); return; }}");
                        }

                        ssb.AppendLine($"v.Serialize(ref writer, options);");
                    }

                    // Deserialize
                    using (var d = ssb.ScopeBrace($"public {x.GoshujinFullName + x.QuestionMarkIfReferenceType} Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
                    {
                        if (x.Kind.IsReferenceType())
                        {// Reference type
                            ssb.AppendLine("if (reader.TryReadNil()) return default;");
                        }

                        ssb.AppendLine($"var v = new {x.GoshujinFullName}();");
                        ssb.AppendLine($"v.Deserialize(ref reader, options);");
                        ssb.AppendLine("return v;");
                    }

                    // Reconstruct
                    using (var r = ssb.ScopeBrace($"public {x.GoshujinFullName} Reconstruct(TinyhandSerializerOptions options)"))
                    {
                        ssb.AppendLine($"var v = new {x.GoshujinFullName}();");
                        ssb.AppendLine("return v;");
                    }
                }

                name = string.Format(classFormat, x.FormatterExtraNumber);
                using (var cls = ssb.ScopeBrace($"class {name}: ITinyhandFormatterExtra<{x.GoshujinFullName}>"))
                {
                    // Deserialize
                    using (var d = ssb.ScopeBrace($"public {x.GoshujinFullName + x.QuestionMarkIfReferenceType} Deserialize({x.GoshujinFullName} reuse, ref TinyhandReader reader, TinyhandSerializerOptions options)"))
                    {
                        if (x.Kind.IsReferenceType() && x.ObjectFlag.HasFlag(CrossLinkObjectFlag.CanCreateInstance))
                        {// Reference type
                            ssb.AppendLine($"reuse = reuse ?? new {x.GoshujinFullName}();");
                        }

                        ssb.AppendLine("reuse.Deserialize(ref reader, options);");
                        ssb.AppendLine("return reuse;");
                    }
                }
            }
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
            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.GenerateINotifyPropertyChanged))
            {
                interfaceString = " : System.ComponentModel.INotifyPropertyChanged";
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
            {// Generate Goshujin
                this.GenerateGoshujinClass(ssb, info);
                this.GenerateGoshujinInstance(ssb, info);
            }

            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.GenerateINotifyPropertyChanged))
            {// Generate PropertyChanged
                ssb.AppendLine("public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
                ssb.AppendLine();
            }

            if (this.ObjectFlag.HasFlag(CrossLinkObjectFlag.TinyhandObject))
            {// Generate SerializeIndex
                ssb.AppendLine($"private int {this.SerializeIndexIdentifier};");
                ssb.AppendLine();
            }

            if (this.Links != null)
            {// Generate Link
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
                            using (var obj = ssb.ScopeObject("this"))
                            {
                                if (x.IsValidLink)
                                {
                                    using (var scopeIf = ssb.ScopeBrace($"if (this.{this.GoshujinInstanceIdentifier} != null)"))
                                    {
                                        this.Generate_AddLink(ssb, info, x, $"this.{this.GoshujinInstanceIdentifier}");
                                    }
                                }

                                if (x.AutoNotify)
                                {
                                    this.Generate_Notify(ssb, info, x);
                                }
                            }
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void Generate_Notify(ScopingStringBuilder ssb, GeneratorInformation info, Linkage link)
        {
            if (this.PropertyChangedDeclaration == DeclarationCondition.ImplicitlyDeclared)
            {
                ssb.AppendLine($"this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(\"{link.Name}\"));");
            }
            else if (this.PropertyChangedDeclaration == DeclarationCondition.ExplicitlyDeclared)
            {
                ssb.AppendLine($"this.{this.ObjectAttribute!.ExplicitPropertyChanged}?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(\"{link.Name}\"));");
            }
        }

        internal void Generate_AddLink(ScopingStringBuilder ssb, GeneratorInformation info, Linkage link, string prefix)
        {
            if (!link.IsValidLink)
            {// Invalid link
                return;
            }
            else if (link.Type == LinkType.Ordered)
            {
                ssb.AppendLine($"{prefix}.{link.ChainName}.Add({ssb.FullObject}.{link.Target!.SimpleName}, {ssb.FullObject});");
            }
            else if (link.Type == LinkType.LinkedList)
            {
                ssb.AppendLine($"{prefix}.{link.ChainName}.AddLast({ssb.FullObject});");
            }
            else if (link.Type == LinkType.StackList)
            {
                ssb.AppendLine($"{prefix}.{link.ChainName}.Push({ssb.FullObject});");
            }
            else if (link.Type == LinkType.QueueList)
            {
                ssb.AppendLine($"{prefix}.{link.ChainName}.Enqueue({ssb.FullObject});");
            }
            else
            {
                ssb.AppendLine($"{prefix}.{link.ChainName}.Add({ssb.FullObject});");
            }
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
            var tinyhandObject = this.ObjectFlag.HasFlag(CrossLinkObjectFlag.TinyhandObject);
            var goshujinInterface = tinyhandObject ? " : ITinyhandSerialize" : string.Empty;
            using (var scopeClass = ssb.ScopeBrace("public sealed class " + goshujinClass + goshujinInterface))
            {
                // Constructor
                // ssb.AppendLine("public " + goshujinClass + "() {}");
                // ssb.AppendLine();

                // this.GenerateGoshujin_Add(ssb, info);
                // this.GenerateGoshujin_Remove(ssb, info);
                this.GenerateGoshujin_Chain(ssb, info);

                if (tinyhandObject)
                {
                    info.UseTinyhand = true;
                    this.FormatterNumber = info.FormatterCount++;
                    this.FormatterExtraNumber = info.FormatterCount++;
                    this.GenerateGoshujin_Tinyhand(ssb, info);

                    if (this.ConstructedObjects != null)
                    {
                        foreach (var x in this.ConstructedObjects)
                        {
                            if (x != this)
                            {// Set closed generic type information for formatter.
                                x.FormatterNumber = info.FormatterCount++;
                                x.FormatterExtraNumber = info.FormatterCount++;
                                x.GoshujinFullName = x.FullName + "." + this.ObjectAttribute!.GoshujinClass;
                            }
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Tinyhand(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine();
            this.GenerateGoshujin_TinyhandSerialize(ssb, info);
            ssb.AppendLine();
            this.GenerateGoshujin_TinyhandDeserialize(ssb, info);
            ssb.AppendLine();
            this.GenerateGoshujin_TinyhandUtf8Name(ssb, info);
        }

        internal void GenerateGoshujin_TinyhandUtf8Name(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.Links == null)
            {
                return;
            }

            foreach (var x in this.Links.Where(x => x.IsValidLink))
            {
                ssb.Append($"private static ReadOnlySpan<byte> {x.ChainNameIdentifier} => new byte[] {{ ");
                foreach (var y in x.ChainNameUtf8)
                {
                    ssb.Append($"{y}, ", false);
                }

                ssb.Append("};\r\n", false);
            }
        }

        internal void GenerateGoshujin_TinyhandSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
        {// void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);
            using (var scopeMethod = ssb.ScopeBrace("public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)"))
            {
                if (this.Links == null)
                {
                    return;
                }

                this.GenerateGoshujin_TinyhandSerialize_ResetIndex(ssb, info);
                this.GenerateGoshujin_TinyhandSerialize_SetIndex(ssb, info);
                this.GenerateGoshujin_TinyhandSerialize_ArrayAndChains(ssb, info);
            }
        }

        internal void GenerateGoshujin_TinyhandSerialize_ResetIndex(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine("var max = 0;");

            foreach (var x in this.Links!.Where(x => x.IsValidLink))
            {
                ssb.AppendLine($"max = max > this.{x.ChainName}.Count ? max : this.{x.ChainName}.Count;");
                using (var scopeFor = ssb.ScopeBrace($"foreach (var x in this.{x.ChainName})"))
                {
                    ssb.AppendLine($"x.{this.SerializeIndexIdentifier} = -1;");
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_TinyhandSerialize_SetIndex(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            ssb.AppendLine($"var array = new {this.LocalName}[max];");
            ssb.AppendLine("var number = 0;");

            foreach (var x in this.Links!.Where(x => x.IsValidLink))
            {
                using (var scopeFor = ssb.ScopeBrace($"foreach (var x in this.{x.ChainName})"))
                {
                    using (var scopeIf = ssb.ScopeBrace($"if (x.{this.SerializeIndexIdentifier} == -1)"))
                    {
                        using (var scopeIf2 = ssb.ScopeBrace("if (number >= max)"))
                        {
                            ssb.AppendLine("max <<= 1;");
                            ssb.AppendLine("Array.Resize(ref array, max);");
                        }

                        ssb.AppendLine("array[number] = x;");
                        ssb.AppendLine($"x.{this.SerializeIndexIdentifier} = number++;");
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_TinyhandSerialize_ArrayAndChains(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            // array header
            ssb.AppendLine("writer.WriteArrayHeader(2);");

            // array
            ssb.AppendLine("writer.WriteArrayHeader(number);");
            ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.LocalName}>();");
            using (var scopeFor = ssb.ScopeBrace("foreach (var x in array)"))
            {
                ssb.AppendLine("formatter.Serialize(ref writer, x, options);");
            }

            // chains
            ssb.AppendLine();
            ssb.AppendLine($"writer.WriteMapHeader({this.NumberOfValidLinks});");
            foreach (var x in this.Links!.Where(x => x.IsValidLink))
            {
                ssb.AppendLine($"writer.WriteString({x.ChainNameIdentifier});");
                ssb.AppendLine($"writer.WriteArrayHeader(this.{x.ChainName}.Count);");
                using (var scopeFor2 = ssb.ScopeBrace($"foreach (var x in this.{x.ChainName})"))
                {
                    ssb.AppendLine($"writer.Write(x.{this.SerializeIndexIdentifier});");
                }
            }
        }

        internal void GenerateGoshujin_TinyhandDeserialize(ScopingStringBuilder ssb, GeneratorInformation info)
        {// void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);
            using (var scopeMethod = ssb.ScopeBrace("public void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)"))
            {
                if (this.DeserializeChainAutomata == null)
                {
                    return;
                }

                // length
                ssb.AppendLine("var length = reader.ReadArrayHeader();");
                ssb.AppendLine("if (length < 2) return;");
                ssb.AppendLine();

                // max, array, formatter
                ssb.AppendLine("var max = reader.ReadArrayHeader();");
                ssb.AppendLine($"var array = new {this.LocalName}[max];");
                using (var security = ssb.ScopeSecurityDepth())
                {
                    ssb.AppendLine($"var formatter = options.Resolver.GetFormatter<{this.LocalName}>();");
                    using (var scopeFor = ssb.ScopeBrace("for (var n = 0; n < max; n++)"))
                    {
                        ssb.AppendLine("array[n] = formatter.Deserialize(ref reader, options)!;");
                    }
                }

                ssb.RestoreSecurityDepth();

                // map, chains
                ssb.AppendLine();
                ssb.AppendLine("var numberOfData = reader.ReadMapHeader2();");
                using (var loop = ssb.ScopeBrace("while (numberOfData-- > 0)"))
                {
                    this.DeserializeChainAutomata.Generate(ssb, info);
                }
            }
        }

        internal void GenerateGoshujin_Add(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var scopeParameter = ssb.ScopeObject("x"))
            using (var scopeMethod = ssb.ScopeBrace($"public void Add({this.LocalName} {ssb.FullObject})"))
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.{this.GoshujinInstanceIdentifier} != null && {ssb.FullObject}.{this.GoshujinInstanceIdentifier} != this)"))
                {
                    ssb.AppendLine($"{ssb.FullObject}.{this.GoshujinInstanceIdentifier}.Remove({ssb.FullObject});");
                }

                ssb.AppendLine($"{ssb.FullObject}.{this.GoshujinInstanceIdentifier} = this;");
                if (this.Links != null)
                {
                    foreach (var link in this.Links)
                    {
                        if (link.AutoLink)
                        {
                            this.Generate_AddLink(ssb, info, link, "this");
                        }
                    }
                }
            }

            ssb.AppendLine();
        }

        internal void GenerateGoshujin_Remove(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            using (var scopeParameter = ssb.ScopeObject("x"))
            using (var scopeMethod = ssb.ScopeBrace($"public bool Remove({this.LocalName} {ssb.FullObject})"))
            {
                using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.{this.GoshujinInstanceIdentifier} == this)"))
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

                    ssb.AppendLine($"{ssb.FullObject}.{this.GoshujinInstanceIdentifier} = default!;");
                    ssb.AppendLine("return true;");
                }

                using (var scopeElse = ssb.ScopeBrace($"else"))
                {
                    ssb.AppendLine("return false;");
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
            var goshujinInstance = this.GoshujinInstanceIdentifier; // goshujin + "Instance";

            using (var scopeProperty = ssb.ScopeBrace($"public {this.ObjectAttribute!.GoshujinClass}? {goshujin}"))
            {
                ssb.AppendLine($"get => this.{goshujinInstance};");
                using (var scopeSet = ssb.ScopeBrace("set"))
                {
                    // using (var scopeEqual = ssb.ScopeBrace($"if (!EqualityComparer<{this.ObjectAttribute!.GoshujinClass}>.Default.Equals(value, this.{goshujinInstance}))"))
                    using (var scopeParamter = ssb.ScopeObject("this"))
                    using (var scopeEqual = ssb.ScopeBrace($"if (value != this.{goshujinInstance})"))
                    {
                        using (var scopeIfNull = ssb.ScopeBrace($"if (this.{goshujinInstance} != null)"))
                        {// Remove Chains
                            if (this.Links != null)
                            {
                                foreach (var link in this.Links)
                                {
                                    if (link.IsValidLink)
                                    {
                                        ssb.AppendLine($"this.{goshujinInstance}.{link.ChainName}.Remove({ssb.FullObject});");
                                    }
                                }
                            }
                        }

                        ssb.AppendLine();
                        ssb.AppendLine($"this.{goshujinInstance} = value;");
                        using (var scopeIfNull2 = ssb.ScopeBrace($"if (value != null)"))
                        {// Add Chains
                            if (this.Links != null)
                            {
                                foreach (var link in this.Links)
                                {
                                    if (link.AutoLink)
                                    {
                                        this.Generate_AddLink(ssb, info, link, "value");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ssb.AppendLine();
            ssb.AppendLine($"private {this.ObjectAttribute!.GoshujinClass}? {goshujinInstance};");
            ssb.AppendLine();
        }
    }
}
