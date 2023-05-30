﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Generator;
using TinyhandGenerator;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink.Generator;

public enum DeclarationCondition
{
    NotDeclared, // Not declared
    ImplicitlyDeclared, // declared (implicitly)
    ExplicitlyDeclared, // declared (explicitly interface)
}

[Flags]
public enum ValueLinkObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    // Link object
    HasLink = 1 << 10, // Has valid link (not ChainType.None)
    HasNotify = 1 << 11, // Has AutoNotify
    CanCreateInstance = 1 << 12, // Can create an instance
    GenerateINotifyPropertyChanged = 1 << 13, // Generate INotifyPropertyChanged
    GenerateSetProperty = 1 << 14, // Generate SetProperty()
    TinyhandObject = 1 << 15, // Has TinyhandObjectAttribute
    HasLinkAttribute = 1 << 16, // Has LinkAttribute
    HasPrimaryLink = 1 << 17, // Has primary link
    GenerateJournaling = 1 << 18, // Generate journaling
    EnableLock = 1 << 19, // Lock
}

public class ValueLinkObject : VisceralObjectBase<ValueLinkObject>
{
    public ValueLinkObject()
    {
    }

    public new ValueLinkBody Body => (ValueLinkBody)((VisceralObjectBase<ValueLinkObject>)this).Body;

    public ValueLinkObjectFlag ObjectFlag { get; private set; }

    public ValueLinkObjectAttributeMock? ObjectAttribute { get; private set; }

    public TinyhandObjectAttributeMock? TinyhandAttribute { get; private set; }

    public DeclarationCondition PropertyChangedDeclaration { get; private set; }

    public List<Linkage>? Links { get; private set; } = null;

    public Linkage? PrimaryLink { get; private set; }

    public int NumberOfValidLinks { get; private set; }

    public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

    public List<ValueLinkObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<ValueLinkObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public string GoshujinInstanceIdentifier = string.Empty;

    public string GoshujinFullName = string.Empty;

    public string SerializeIndexIdentifier = string.Empty;

    public ValueLinkObject? ClosedGenericHint { get; private set; }

    internal Automata<ValueLinkObject, Linkage>? DeserializeChainAutomata { get; private set; }

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
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= ValueLinkObjectFlag.Configured;

        // Open generic type is not supported.
        /*var genericsType = this.Generics_Kind;
        if (genericsType == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }*/

        // Closed generic type is not supported.
        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {
            if (this.OriginalDefinition != null && this.OriginalDefinition.ClosedGenericHint == null)
            {
                this.OriginalDefinition.ClosedGenericHint = this;
            }

            return;
        }

        // ValueLinkObjectAttribute
        if (this.AllAttributes.FirstOrDefault(x => x.FullName == ValueLinkObjectAttributeMock.FullName) is { } objectAttribute)
        {
            this.Location = objectAttribute.Location;
            try
            {
                this.ObjectAttribute = ValueLinkObjectAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);

                // Goshujin Class / Instance
                this.ObjectAttribute.GoshujinClass = (this.ObjectAttribute.GoshujinClass != string.Empty) ? this.ObjectAttribute.GoshujinClass : ValueLinkBody.DefaultGoshujinClass;
                this.ObjectAttribute.GoshujinInstance = (this.ObjectAttribute.GoshujinInstance != string.Empty) ? this.ObjectAttribute.GoshujinInstance : ValueLinkBody.DefaultGoshujinInstance;
                this.ObjectAttribute.ExplicitPropertyChanged = (this.ObjectAttribute.ExplicitPropertyChanged != string.Empty) ? this.ObjectAttribute.ExplicitPropertyChanged : ValueLinkBody.ExplicitPropertyChanged;
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(ValueLinkBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }

        // Linkage
        foreach (var linkAttribute in this.AllAttributes.Where(x => x.FullName == LinkAttributeMock.FullName))
        {
            var linkage = Linkage.Create(this, linkAttribute);
            if (linkage == null)
            {
                continue;
            }

            this.ObjectFlag |= ValueLinkObjectFlag.HasLinkAttribute;
            if (this.ContainingObject is { } parent)
            {// Add to parent's list
                if (parent.Links == null)
                {
                    parent.Links = new();
                }

                if (linkage.Target != null && parent.Links.FirstOrDefault(x => x.Target == linkage.Target) is { } mainLink)
                {
                    // Multiple linkages.
                    linkage.MainLink = mainLink;
                    linkage.ValueName = mainLink.ValueName;
                }

                parent.Links.Add(linkage);
            }
        }

        // TinyhandObjectAttribute
        if (this.AllAttributes.FirstOrDefault(x => x.FullName == TinyhandObjectAttributeMock.FullName) is { } tinyhandAttribute)
        {
            try
            {
                this.TinyhandAttribute = TinyhandObjectAttributeMock.FromArray(tinyhandAttribute.ConstructorArguments, tinyhandAttribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(ValueLinkBody.Error_AttributePropertyError, tinyhandAttribute.Location);
            }

            this.ObjectFlag |= ValueLinkObjectFlag.TinyhandObject;
            if (this.TinyhandAttribute?.Journaling == true)
            {
                this.ObjectFlag |= ValueLinkObjectFlag.GenerateJournaling;
            }
        }

        if (this.ObjectAttribute != null)
        {// ValueLinkObject
            if (this.ObjectAttribute.Lock)
            {// Lock
                this.ObjectFlag |= ValueLinkObjectFlag.EnableLock;
            }

            this.ConfigureObject();
        }
    }

    private void ConfigureObject()
    {
        // Used keywords
        this.Identifier = new VisceralIdentifier("__gen_cl_identifier__"); // Don't forget CrossLink!
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

            if (flag && x.TypeObject != null && !x.IsStatic && x.ContainingObject == this)
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
                    this.ObjectFlag |= ValueLinkObjectFlag.HasLink;
                }

                if (x.AutoNotify)
                {
                    this.ObjectFlag |= ValueLinkObjectFlag.HasNotify;
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
                this.Body.AddDiagnostic(ValueLinkBody.Warning_PropertyChanged, this.Location, this.SimpleName);
            }
        }
        else if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.HasNotify))
        {// Generate INotifyPropertyChanged
            this.ObjectFlag |= ValueLinkObjectFlag.GenerateINotifyPropertyChanged;
            this.PropertyChangedDeclaration = DeclarationCondition.ImplicitlyDeclared;
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateINotifyPropertyChanged))
        {
            if (!this.Has_SetProperty())
            {
                this.ObjectFlag |= ValueLinkObjectFlag.GenerateSetProperty;
            }
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
        {// TinyhandObject
            this.SerializeIndexIdentifier = this.Identifier.GetIdentifier();
        }
    }

    public bool Has_SetProperty()
    {
        foreach (var x in this.GetMembers(VisceralTarget.Method).Where(x => x.SimpleName == "SetProperty"))
        {
            var p = x.Method_Parameters;
            if (p.Length != 3)
            {
                continue;
            }

            if (p[0] == p[1] && p[2] == "string")
            {
                return true;
            }
        }

        return false;
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= ValueLinkObjectFlag.RelationConfigured;

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
            List<ValueLinkObject>? list;
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

        if (cf.ConstructedObjects == null)
        {
            cf.ConstructedObjects = new();
        }

        if (!cf.ConstructedObjects.Contains(this))
        {
            cf.ConstructedObjects.Add(this);
        }
    }

    public void CheckObject()
    {
        if (!this.IsAbstractOrInterface)
        {
            this.ObjectFlag |= ValueLinkObjectFlag.CanCreateInstance;
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.CanCreateInstance))
        {// Type which can create an instance
            // partial class required.
            if (!this.IsPartial)
            {
                this.Body.ReportDiagnostic(ValueLinkBody.Error_NotPartial, this.Location, this.FullName);
            }

            // Parent class also needs to be a partial class.
            var parent = this.ContainingObject;
            while (parent != null)
            {
                if (!parent.IsPartial)
                {
                    this.Body.ReportDiagnostic(ValueLinkBody.Error_NotPartialParent, parent.Location, parent.FullName);
                }

                parent = parent.ContainingObject;
            }
        }

        // Check base class.
        var baseObject = this.BaseObject;
        while (baseObject != null)
        {
            if (baseObject.ObjectAttribute != null)
            {
                this.Body.ReportDiagnostic(ValueLinkBody.Error_DerivedClass, this.Location, this.FullName, baseObject.FullName);
                return;
            }

            baseObject = baseObject.BaseObject;
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.HasLink))
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

                var result = true;
                if (!x.NoValue && x.MainLink == null)
                {
                    result = this.CheckKeyword(x.ValueName, x.Location);
                }

                if (x.IsValidLink && result)
                {
                    this.CheckKeyword(x.LinkName, x.Location);
                    this.CheckKeyword(x.ChainName, x.Location);
                    this.NumberOfValidLinks++;
                }

                // Check
                if (x.Target == null)
                {
                    if (x.RequiresTarget)
                    {
                        this.Body.AddDiagnostic(ValueLinkBody.Error_NoLinkTarget, x.Location);
                    }

                    if (x.AutoNotify)
                    {
                        this.Body.AddDiagnostic(ValueLinkBody.Error_NoNotifyTarget, x.Location);
                    }
                }
            }
        }

        // Check primary link
        if (this.Links != null)
        {
            this.PrimaryLink = this.Links.FirstOrDefault(x => x.Primary);
            if (this.TinyhandAttribute?.Journaling == true)
            {// Required
                if (this.PrimaryLink is null || !this.PrimaryLink.Type.IsLocatable())
                {
                    this.Body.AddDiagnostic(ValueLinkBody.Error_NoPrimaryLink, this.Location);
                    return;
                }
            }

            if (this.PrimaryLink != null)
            {// Has primary link.
                this.ObjectFlag |= ValueLinkObjectFlag.HasPrimaryLink;
            }
            else if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
            {// No primary link
                this.Body.AddDiagnostic(ValueLinkBody.Warning_NoPrimaryLink, this.Location);
            }
        }
    }

    public void CheckMember(ValueLinkObject parent)
    {
        // Avoid this.TypeObject!
        if (this.TypeObject == null)
        {
            return;
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.HasLinkAttribute))
        {
            if (!this.IsSerializable || this.IsReadOnly || this.IsInitOnly)
            {// Not serializable
                this.Body.AddDiagnostic(ValueLinkBody.Error_ReadonlyMember, this.Location, this.SimpleName);
            }

            /*if (this.Kind != VisceralObjectKind.Field)
            {// Link target must be a field
                this.Body.AddDiagnostic(ValueLinkBody.Error_LinkTargetNotField, this.Location, this.SimpleName);
            }*/

            if (parent.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
            {// TinyhandObject
                if (!this.AllAttributes.Any(x =>
                x.FullName == "Tinyhand.KeyAttribute" ||
                x.FullName == "Tinyhand.KeyAsNameAttribute"))
                {
                    this.Body.AddDiagnostic(ValueLinkBody.Warning_NoKeyAttribute, this.Location);
                }
            }
        }
    }

    public bool CheckKeyword(string keyword, Location? location = null)
    {
        if (!this.Identifier.Add(keyword))
        {
            this.Body.AddDiagnostic(ValueLinkBody.Error_KeywordUsed, location ?? Location.None, this.SimpleName, keyword);
            return false;
        }

        return true;
    }

    public void Check()
    {
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.Checked))
        {
            return;
        }

        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {// Close generic is not necessary.
            return;
        }

        this.ObjectFlag |= ValueLinkObjectFlag.Checked;

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
            if (ret.KeyResized)
            {// Key resized
                this.Body.AddDiagnostic(ValueLinkBody.Warning_StringKeySizeLimit, x.Location, Automata<ValueLinkObject, Linkage>.MaxStringKeySizeInBytes);
            }

            if (ret.Result == AutomataAddNodeResult.KeyCollision)
            {// Key collision
                this.Body.AddDiagnostic(ValueLinkBody.Error_StringKeyConflict, x.Location);
                if (ret.Node != null && ret.Node.Member != null)
                {
                    this.Body.AddDiagnostic(ValueLinkBody.Error_StringKeyConflict, ret.Node.Member.Location);
                }

                continue;
            }
            else if (ret.Result == AutomataAddNodeResult.NullKey)
            {// Null key
                this.Body.AddDiagnostic(ValueLinkBody.Error_StringKeyNull, x.Location);
                continue;
            }
            else if (ret.Node == null)
            {
                continue;
            }
        }
    }

    public static void GenerateDeserializeChain(ValueLinkObject obj, ScopingStringBuilder ssb, object? info, Linkage link)
    {
        if (link.AutoLink)
        {
            ssb.AppendLine($"read{link.ChainName} = true;"); // readflag
        }

        ssb.AppendLine("var len = reader.ReadArrayHeader();");
        ssb.AppendLine($"{ssb.FullObject}.{link.ChainName}.Clear();");
        var prevObject = ssb.FullObject;
        using (var scopeParameter = ssb.ScopeFullObject("x"))
        using (var scopeFor = ssb.ScopeBrace("for (var n = 0; n < len; n++)"))
        {
            ssb.AppendLine("var i = reader.ReadInt32();");
            ssb.AppendLine("if (i >= max) throw new IndexOutOfRangeException();");
            ssb.AppendLine("var x = array[i];");
            obj.Generate_AddLink(ssb, (GeneratorInformation)info!, link, prevObject);
        }
    }

    internal void GenerateFlatLoader(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ObjectAttribute == null)
        {
        }
        else if (this.ConstructedObjects == null)
        {
        }
        else if (!this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
        {
        }
        else
        {
            this.GenerateLoaderCore(ssb, info, true);
        }

        if (this.Children?.Count > 0)
        {
            foreach (var x in this.Children)
            {
                x.GenerateFlatLoader(ssb, info);
            }
        }
    }

    internal void GenerateLoaderCore(ScopingStringBuilder ssb, GeneratorInformation info, bool checkAccessibility)
    {
        var isAccessible = true;
        if (checkAccessibility && this.ContainsNonPublicObject())
        {
            isAccessible = false;
        }

        if (this.Generics_Kind != VisceralGenericsKind.OpenGeneric)
        {// FormatterContainsNonPublic
            if (isAccessible)
            {
                ssb.AppendLine($"GeneratedResolver.Instance.SetFormatter(new Tinyhand.Formatters.TinyhandObjectFormatter<{this.GoshujinFullName}>());");
            }
            else
            {
                var fullName = this.GetGenericsName();
                ssb.AppendLine($"GeneratedResolver.Instance.SetFormatterGenerator(Type.GetType(\"{fullName}+{this.ObjectAttribute!.GoshujinClass}\")!, static (x, y) =>");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                ssb.AppendLine($"var formatter = Activator.CreateInstance(typeof(Tinyhand.Formatters.TinyhandObjectFormatter<>).MakeGenericType(x));");
                ssb.AppendLine("return (ITinyhandFormatter)formatter!;");

                ssb.DecrementIndent();
                ssb.AppendLine("});");
            }
        }
        else
        {// Formatter generator
            string typeName;
            if (isAccessible)
            {
                var generic = this.GetClosedGenericName(null);
                typeName = $"typeof({generic.Name}.{this.ObjectAttribute!.GoshujinClass})";
            }
            else
            {
                var fullName = this.GetGenericsName();
                typeName = $"Type.GetType(\"{fullName}+{this.ObjectAttribute!.GoshujinClass}\")!";
            }

            ssb.AppendLine($"GeneratedResolver.Instance.SetFormatterGenerator({typeName}, static (x, y) =>");
            ssb.AppendLine("{");
            ssb.IncrementIndent();

            ssb.AppendLine($"var ft = x.MakeGenericType(y);");
            ssb.AppendLine($"var formatter = Activator.CreateInstance(typeof(Tinyhand.Formatters.TinyhandObjectFormatter<>).MakeGenericType(ft));");
            ssb.AppendLine("return (ITinyhandFormatter)formatter!;");

            ssb.DecrementIndent();
            ssb.AppendLine("});");
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
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateINotifyPropertyChanged))
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
            }
        }
    }

    internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.HasLink))
        {// Generate Goshujin
            this.GenerateGoshujinClass(ssb, info);
            this.GenerateGoshujinInstance(ssb, info);
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateINotifyPropertyChanged))
        {// Generate PropertyChanged
            ssb.AppendLine("public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
            if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateSetProperty))
            {
                this.Generate_SetProperty(ssb, info);
            }

            ssb.AppendLine();
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
        {// Generate SerializeIndex
            ssb.AppendLine($"private int {this.SerializeIndexIdentifier};");
            ssb.AppendLine();
        }

        if (this.Links != null)
        {// Generate Value and Link properties
            // Main link, sub, sub,,,
            foreach (var priority in this.Links.Where(a => a.MainLink == null))
            {
                var sub = this.Links.Where(a => a.MainLink == priority).ToArray();
                this.GenerateLink(ssb, info, priority, sub);
            }
        }

        return;
    }

    internal void Generate_SetProperty(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace("protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)"))
        {
            using (var scopeIf = ssb.ScopeBrace("if (EqualityComparer<T>.Default.Equals(storage, value))"))
            {
                ssb.AppendLine("return false;");
            }

            ssb.AppendLine("storage = value;");
            ssb.AppendLine("this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));");
            ssb.AppendLine("return true;");
        }
    }

    internal void GenerateLink(ScopingStringBuilder ssb, GeneratorInformation info, Linkage main, Linkage[] sub)
    {
        var tinyhandProperty = this.TinyhandAttribute is not null &&
            main.Target?.AllAttributes.Any(x => x.FullName == Tinyhand.Generator.KeyAttributeMock.FullName || x.FullName == Tinyhand.Generator.KeyAsNameAttributeMock.FullName) == true;

        if (!main.NoValue/* && !tinyhandProperty*/)
        {// Value property
            this.GenerateLink_Property(ssb, info, main, sub);
        }

        if (tinyhandProperty)
        {
            this.GenerateLink_Update(ssb, info, main, sub);
        }

        ssb.AppendLine();

        this.GenerateLink_Link(ssb, info, main);
        foreach (var x in sub)
        {
            this.GenerateLink_Link(ssb, info, x);
        }
    }

    internal void GenerateLink_Update(ScopingStringBuilder ssb, GeneratorInformation info, Linkage main, Linkage[] sub)
    {
        using (var scopeUpdate = ssb.ScopeBrace($"private void __gen_cl_update_{main.TargetName}()"))
        {
            using (var obj = ssb.ScopeObject("this"))
            {
                if (main.IsValidLink)
                {
                    this.Generate_AddLink(ssb, info, main, $"this.{this.GoshujinInstanceIdentifier}?");
                }

                foreach (var x in sub.Where(a => a.IsValidLink))
                {
                    this.Generate_AddLink(ssb, info, x, $"this.{this.GoshujinInstanceIdentifier}?");
                }
            }
        }
    }

    internal void GenerateLink_Property(ScopingStringBuilder ssb, GeneratorInformation info, Linkage main, Linkage[] sub)
    {
        var target = main.Target;
        if (target == null || target.TypeObject == null)
        {
            return;
        }

        var accessibility = VisceralHelper.GetterSetterAccessibilityToPropertyString(main.GetterAccessibility, main.SetterAccessibility);
        using (var scopeProperty = ssb.ScopeBrace($"{accessibility.Property}{target.TypeObject.FullName} {main.ValueName}"))
        {
            ssb.AppendLine($"{accessibility.Getter}get => this.{main.TargetName};");
            using (var scopeSet = ssb.ScopeBrace($"{accessibility.Setter}set"))
            {
                string compare;
                if (target.TypeObject.IsPrimitive)
                {
                    compare = $"if (value != this.{main.TargetName})";
                }
                else
                {
                    compare = $"if (!EqualityComparer<{target.TypeObject.FullName}>.Default.Equals(value, this.{main.TargetName}))";
                }

                using (var scopeCompare = ssb.ScopeBrace(compare))
                {
                    ssb.AppendLine($"this.{main.TargetName} = value;");

                    if (main.AutoLink)
                    {
                        using (var obj = ssb.ScopeObject("this"))
                        {
                            if (main.IsValidLink)
                            {
                                this.Generate_AddLink(ssb, info, main, $"this.{this.GoshujinInstanceIdentifier}?");
                            }

                            foreach (var x in sub.Where(a => a.IsValidLink))
                            {
                                this.Generate_AddLink(ssb, info, x, $"this.{this.GoshujinInstanceIdentifier}?");
                            }

                            if (main.AutoNotify)
                            {
                                this.Generate_Notify(ssb, info, main);
                            }
                        }
                    }
                }
            }
        }
    }

    internal void GenerateLink_Link(ScopingStringBuilder ssb, GeneratorInformation info, Linkage x)
    {
        if (x.Type == ChainType.None)
        {
            return;
        }
        else if (x.RequiresTarget && x.Target != null && x.Target.TypeObject != null)
        {
            ssb.AppendLine($"{x.GetterAccessibility.AccessibilityToString()} {x.Type.ChainTypeToName()}<{x.Target!.TypeObject!.FullName}, {this.LocalName}>.Link {x.LinkName};");
        }
        else
        {
            ssb.AppendLine($"{x.GetterAccessibility.AccessibilityToString()} {x.Type.ChainTypeToName()}<{this.LocalName}>.Link {x.LinkName};");
        }

        ssb.AppendLine();
    }

    internal void Generate_Notify(ScopingStringBuilder ssb, GeneratorInformation info, Linkage link)
    {
        if (this.PropertyChangedDeclaration == DeclarationCondition.ImplicitlyDeclared)
        {
            ssb.AppendLine($"this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(\"{link.ValueName}\"));");
        }
        else if (this.PropertyChangedDeclaration == DeclarationCondition.ExplicitlyDeclared)
        {
            ssb.AppendLine($"this.{this.ObjectAttribute!.ExplicitPropertyChanged}?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(\"{link.ValueName}\"));");
        }
    }

    internal void Generate_AddLink(ScopingStringBuilder ssb, GeneratorInformation info, Linkage link, string prefix)
    {
        if (!link.IsValidLink)
        {// Invalid link
            return;
        }

        ScopingStringBuilder.IScope? scopePredicate = null;
        if (link.PredicateMethodName != null)
        {
            scopePredicate = ssb.ScopeBrace($"if ({ssb.FullObject}.{link.PredicateMethodName}())");
        }

        if (link.Type == ChainType.Ordered || link.Type == ChainType.ReverseOrdered || link.Type == ChainType.Unordered)
        {
            ssb.AppendLine($"{prefix}.{link.ChainName}.Add({ssb.FullObject}.{link.Target!.SimpleName}, {ssb.FullObject});");
        }
        else if (link.Type == ChainType.LinkedList)
        {
            ssb.AppendLine($"{prefix}.{link.ChainName}.AddLast({ssb.FullObject});");
        }
        else if (link.Type == ChainType.StackList)
        {
            ssb.AppendLine($"{prefix}.{link.ChainName}.Push({ssb.FullObject});");
        }
        else if (link.Type == ChainType.QueueList)
        {
            ssb.AppendLine($"{prefix}.{link.ChainName}.Enqueue({ssb.FullObject});");
        }
        else
        {
            ssb.AppendLine($"{prefix}.{link.ChainName}.Add({ssb.FullObject});");
        }

        if (link.AddedMethodName != null)
        {
            ssb.AppendLine($"{ssb.FullObject}.{link.AddedMethodName}();");
        }

        if (scopePredicate != null)
        {
            scopePredicate.Dispose();
        }
    }

    internal void GenerateGoshujinClass(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        var goshujinInterface = " : IGoshujin";

        if (this.PrimaryLink != null)
        {// IEnumerable
            goshujinInterface += $", IEnumerable, IEnumerable<{this.LocalName}>";
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
        {// ITinyhandSerialize
            goshujinInterface += $", ITinyhandSerialize<{this.GoshujinFullName}>, ITinyhandReconstruct<{this.GoshujinFullName}>, ITinyhandClone<{this.GoshujinFullName}>, ITinyhandSerialize";
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateJournaling))
        {// ITinyhandSerialize
            goshujinInterface += $", ITinyhandJournal";
        }

        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.EnableLock))
        {// ILockable
            goshujinInterface += $", Arc.Threading.ILockable";
        }

        using (var scopeClass = ssb.ScopeBrace("public sealed class " + this.ObjectAttribute!.GoshujinClass + goshujinInterface))
        {
            // Constructor
            this.GenerateGoshujin_Constructor(ssb, info);

            this.GenerateGoshujin_Add(ssb, info);
            this.GenerateGoshujin_Remove(ssb, info);
            this.GenerateGoshujin_Clear(ssb, info);
            this.GenerateGoshujin_Chain(ssb, info);

            if (this.PrimaryLink is not null)
            {// IEnumerable, Count
                ssb.AppendLine();
                ssb.AppendLine($"IEnumerator<{this.LocalName}> IEnumerable<{this.LocalName}>.GetEnumerator() => this.{this.PrimaryLink.ChainName}.GetEnumerator();");
                ssb.AppendLine($"System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.{this.PrimaryLink.ChainName}.GetEnumerator();");
                ssb.AppendLine($"public int Count => this.{this.PrimaryLink.ChainName}.Count;");
            }

            if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
            {
                info.UseTinyhand = true;
                this.GenerateGoshujin_Tinyhand(ssb, info);

                if (this.ConstructedObjects != null)
                {
                    foreach (var x in this.ConstructedObjects)
                    {
                        if (x != this)
                        {// Set closed generic type information for formatter.
                            x.GoshujinFullName = x.FullName + "." + this.ObjectAttribute!.GoshujinClass;
                        }
                    }
                }

                if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateJournaling))
                {
                    this.GenerateGosjujin_Journal(ssb, info);
                }
            }

            if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.EnableLock))
            {// ILockable
                this.GenerateGosjujin_Lock(ssb, info);
            }
        }

        ssb.AppendLine();
    }

    internal void GenerateGoshujin_Constructor(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"public {this.ObjectAttribute!.GoshujinClass}()"))
        {
            if (this.Links == null)
            {
                return;
            }

            foreach (var link in this.Links)
            {
                if (link.Type == ChainType.None)
                {
                    continue;
                }

                if (link.Type == ChainType.ReverseOrdered)
                {
                    ssb.AppendLine($"this.{link.ChainName} = new(this, static x => x.{this.GoshujinInstanceIdentifier}, static x => ref x.{link.LinkName}, true);");
                }
                else
                {
                    ssb.AppendLine($"this.{link.ChainName} = new(this, static x => x.{this.GoshujinInstanceIdentifier}, static x => ref x.{link.LinkName});");
                }
            }
        }

        ssb.AppendLine();
    }

    internal void GenerateGosjujin_Lock(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine("public Arc.Threading.ILockable LockObject { get; set; } = new Arc.Threading.MonitorLock();");
        ssb.AppendLine("public bool IsLocked => this.LockObject.IsLocked;");
        ssb.AppendLine("public bool Enter() => this.LockObject.Enter();");
        ssb.AppendLine("public void Exit() => this.LockObject.Exit();");
        ssb.AppendLine("public Arc.Threading.LockStruct Lock() => new(this);");
    }

    internal void GenerateGosjujin_Journal(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.PrimaryLink?.Target?.TypeObject is null)
        {
            return;
        }

        ssb.AppendLine();

        ssb.AppendLine("[IgnoreMember] public ITinyhandCrystal? Crystal { get; set; }");
        ssb.AppendLine("[IgnoreMember] public uint CurrentPlane { get; set; }");

        using (var scopeMethod = ssb.ScopeBrace("bool ITinyhandJournal.ReadRecord(ref TinyhandReader reader)"))
        {
            ssb.AppendLine("var record = reader.Read_Record();");

            using (var scopeLocator = ssb.ScopeBrace("if (record == JournalRecord.Locator)"))
            {// Locator
                var typeObject = this.PrimaryLink.Target.TypeObject;
                ssb.AppendLine($"var key = {typeObject.CodeReader()};");
                var keyIsNotNull = typeObject.Kind.IsValueType() ? string.Empty : "key is not null && ";
                ssb.AppendLine($"if ({keyIsNotNull}this.{this.PrimaryLink.ChainName}.FindFirst(key) is ITinyhandJournal obj)");

                ssb.AppendLine("{");
                ssb.IncrementIndent();
                ssb.AppendLine("return obj.ReadRecord(ref reader);");
                ssb.DecrementIndent();
                ssb.AppendLine("}");
            }

            using (var scopeAdd = ssb.ScopeBrace("else if (record == JournalRecord.Add)"))
            {// Add
                ssb.AppendLine("if (reader.TryReadBytes(out var span))");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                ssb.AppendLine("try");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                ssb.AppendLine($"var obj = TinyhandSerializer.DeserializeObject<{this.LocalName}>(span);");
                ssb.AppendLine("if (obj is not null)");
                ssb.AppendLine("{");
                ssb.IncrementIndent();
                ssb.AppendLine($"obj.{this.ObjectAttribute?.GoshujinInstance} = this;");
                ssb.AppendLine("return true;");
                ssb.DecrementIndent();
                ssb.AppendLine("}");

                ssb.DecrementIndent();
                ssb.AppendLine("}");
                ssb.AppendLine("catch {}");

                ssb.DecrementIndent();
                ssb.AppendLine("}");
            }

            using (var scopeRemove = ssb.ScopeBrace("else if (record == JournalRecord.Remove)"))
            {// Remove
                var typeObject = this.PrimaryLink.Target.TypeObject;
                ssb.AppendLine($"var key = {typeObject.CodeReader()};");
                var keyIsNotNull = typeObject.Kind.IsValueType() ? string.Empty : "key is not null && ";
                ssb.AppendLine($"if ({keyIsNotNull}this.{this.PrimaryLink.ChainName}.FindFirst(key) is {{ }} obj)");

                ssb.AppendLine("{");
                ssb.IncrementIndent();
                ssb.AppendLine($"obj.{this.ObjectAttribute?.GoshujinInstance} = null;");
                ssb.AppendLine("return true;");
                ssb.DecrementIndent();
                ssb.AppendLine("}");
            }

            ssb.AppendLine("return false;");
        }
    }

    internal void GenerateGoshujin_Tinyhand(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine();
        this.GenerateGoshujin_TinyhandSerialize(ssb, info);
        ssb.AppendLine();
        this.GenerateGoshujin_TinyhandDeserialize(ssb, info);
        ssb.AppendLine();
        this.GenerateGoshujin_TinyhandReconstruct(ssb, info);
        ssb.AppendLine();
        this.GenerateGoshujin_TinyhandClone(ssb, info);
        ssb.AppendLine();
        this.GenerateGoshujin_TinyhandITinyhandSerialize(ssb, info);
    }

    internal void GenerateGoshujin_TinyhandSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {// void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options);
        using (var scopeMethod = ssb.ScopeBrace($"static void ITinyhandSerialize<{this.GoshujinFullName}>.Serialize(ref TinyhandWriter writer, scoped ref {this.GoshujinFullName}? v, TinyhandSerializerOptions options)"))
        using (var v = ssb.ScopeObject("v"))
        {
            if (this.Kind.IsReferenceType())
            {
                using (var scopeNullCheck = ssb.ScopeBrace($"if ({ssb.FullObject} == null)"))
                {
                    ssb.AppendLine("writer.WriteNil();");
                    ssb.AppendLine("return;");
                }

                ssb.AppendLine();
            }

            if (this.Links == null)
            {
                return;
            }

            var lockScope = this.ScopeLock(ssb);

            var primaryLink = this.Links.FirstOrDefault(x => x.Primary);
            if (primaryLink != null)
            {// Primary link
                this.GenerateGoshujin_TinyhandSerialize_PrimaryIndex(ssb, info, primaryLink);
            }
            else
            {// No primary link
                this.GenerateGoshujin_TinyhandSerialize_ResetIndex(ssb, info);
                this.GenerateGoshujin_TinyhandSerialize_SetIndex(ssb, info);
            }

            this.GenerateGoshujin_TinyhandSerialize_ArrayAndChains(ssb, info);

            lockScope?.Dispose();
        }
    }

    internal void GenerateGoshujin_TinyhandSerialize_PrimaryIndex(ScopingStringBuilder ssb, GeneratorInformation info, Linkage link)
    {
        ssb.AppendLine($"var max = {ssb.FullObject}.{link.ChainName}.Count;");
        ssb.AppendLine("var number = 0;");
        ssb.AppendLine($"var array = new {this.LocalName}[max];");

        using (var scopeFor = ssb.ScopeBrace($"foreach (var x in {ssb.FullObject}.{link.ChainName})"))
        {
            ssb.AppendLine("array[number] = x;");
            ssb.AppendLine($"x.{this.SerializeIndexIdentifier} = number++;");
        }

        ssb.AppendLine();
    }

    internal void GenerateGoshujin_TinyhandSerialize_ResetIndex(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine("var max = 0;");

        foreach (var x in this.Links!.Where(x => x.IsValidLink))
        {
            ssb.AppendLine($"max = max > {ssb.FullObject}.{x.ChainName}.Count ? max : {ssb.FullObject}.{x.ChainName}.Count;");
            using (var scopeFor = ssb.ScopeBrace($"foreach (var x in {ssb.FullObject}.{x.ChainName})"))
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
            using (var scopeFor = ssb.ScopeBrace($"foreach (var x in {ssb.FullObject}.{x.ChainName})"))
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
            ssb.AppendLine($"writer.WriteString(\"{x.ChainName}\"u8);");
            ssb.AppendLine($"writer.WriteArrayHeader({ssb.FullObject}.{x.ChainName}.Count);");
            using (var scopeFor2 = ssb.ScopeBrace($"foreach (var x in {ssb.FullObject}.{x.ChainName})"))
            {
                ssb.AppendLine($"writer.Write(x.{this.SerializeIndexIdentifier});");
            }
        }
    }

    internal void GenerateGoshujin_TinyhandDeserialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {// void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options);
        using (var scopeMethod = ssb.ScopeBrace($"static void ITinyhandSerialize<{this.GoshujinFullName}>.Deserialize(ref TinyhandReader reader, scoped ref {this.GoshujinFullName}? v, TinyhandSerializerOptions options)"))
        using (var v = ssb.ScopeObject("v"))
        {
            if (this.Kind.IsReferenceType())
            {
                using (var scopeNillCheck = ssb.ScopeBrace($"if (reader.TryReadNil())"))
                {
                    ssb.AppendLine("return;");
                }

                ssb.AppendLine();
                ssb.AppendLine($"{ssb.FullObject} ??= new();");
            }

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
                    ssb.AppendLine($"array[n].{this.GoshujinInstanceIdentifier} = {ssb.FullObject};");
                }
            }

            ssb.RestoreSecurityDepth();

            // readflag
            if (this.Links != null)
            {
                ssb.AppendLine();
                foreach (var x in this.Links.Where(x => x.IsValidLink && x.AutoLink))
                {
                    ssb.AppendLine($"var read{x.ChainName} = false;");
                }
            }

            // map, chains
            ssb.AppendLine();
            ssb.AppendLine("var numberOfData = reader.ReadMapHeader2();");
            using (var loop = ssb.ScopeBrace("while (numberOfData-- > 0)"))
            {
                this.DeserializeChainAutomata.Generate(ssb, info);
            }

            // readflag
            if (this.Links != null)
            {// autolink unread chains.
                foreach (var x in this.Links.Where(x => x.IsValidLink && x.AutoLink))
                {
                    ssb.AppendLine();
                    using (var ifUnread = ssb.ScopeBrace($"if (!read{x.ChainName})"))
                    {
                        using (var scopeFor = ssb.ScopeBrace("for (var n = 0; n < max; n++)"))
                        {
                            var prevObject = ssb.FullObject;
                            using (var scopeParameter = ssb.ScopeFullObject("x"))
                            {
                                ssb.AppendLine("var x = array[n];");
                                this.Generate_AddLink(ssb, info, x, prevObject);
                            }
                        }
                    }
                }
            }
        }
    }

    internal void GenerateGoshujin_TinyhandReconstruct(ScopingStringBuilder ssb, GeneratorInformation info)
    {// this.GoshujinFullName? Clone(TinyhandSerializerOptions options);
        using (var scopeMethod = ssb.ScopeBrace($"static void ITinyhandReconstruct<{this.GoshujinFullName}>.Reconstruct([NotNull] scoped ref {this.GoshujinFullName}? v, TinyhandSerializerOptions options)"))
        {
            ssb.AppendLine($"v = new {this.GoshujinFullName}();");
        }
    }

    internal void GenerateGoshujin_TinyhandClone(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"static {this.GoshujinFullName}? ITinyhandClone<{this.GoshujinFullName}>.Clone(scoped ref {this.GoshujinFullName}? v, TinyhandSerializerOptions options)"))
        {
            ssb.AppendLine($"return v == null ? null : TinyhandSerializer.Deserialize<{this.GoshujinFullName}>(TinyhandSerializer.Serialize(v));");
        }
    }

    internal void GenerateGoshujin_TinyhandITinyhandSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {// ITinyhandSerialize
        ssb.AppendLine("void ITinyhandSerialize.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)");
        ssb.AppendLine("    => TinyhandSerializer.DeserializeObject(ref reader, ref Unsafe.AsRef(this)!, options);");
        ssb.AppendLine("void ITinyhandSerialize.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)");
        ssb.AppendLine("    => TinyhandSerializer.SerializeObject(ref writer, this, options);");
    }

    internal void GenerateGoshujin_Add(ScopingStringBuilder ssb, GeneratorInformation info)
    {// I've implemented this feature, but I'm wondering if I should enable it due to my coding philosophy.
        using (var scopeParameter = ssb.ScopeObject("x"))
        using (var scopeMethod = ssb.ScopeBrace($"public void Add({this.LocalName} {ssb.FullObject})"))
        {
            ssb.AppendLine($"{ssb.FullObject}.{this.ObjectAttribute!.GoshujinInstance} = this;");
        }

        ssb.AppendLine();

        // Obsolete
        /* using (var scopeParameter = ssb.ScopeObject("x"))
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

        ssb.AppendLine();*/
    }

    internal void GenerateGoshujin_Remove(ScopingStringBuilder ssb, GeneratorInformation info)
    {// I've implemented this feature, but I'm wondering if I should enable it due to my coding philosophy.
        using (var scopeParameter = ssb.ScopeObject("x"))
        using (var scopeMethod = ssb.ScopeBrace($"public bool Remove({this.LocalName}? {ssb.FullObject})"))
        {
            using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}?.{this.GoshujinInstanceIdentifier} == this)"))
            {
                ssb.AppendLine($"{ssb.FullObject}.{this.ObjectAttribute!.GoshujinInstance} = null;");
                ssb.AppendLine("return true;");
            }

            using (var scopeElse = ssb.ScopeBrace($"else"))
            {
                ssb.AppendLine("return false;");
            }
        }

        ssb.AppendLine();

        // Obsolete
        /* using (var scopeParameter = ssb.ScopeObject("x"))
        using (var scopeMethod = ssb.ScopeBrace($"public bool Remove({this.LocalName} {ssb.FullObject})"))
        {
            using (var scopeIf = ssb.ScopeBrace($"if ({ssb.FullObject}.{this.GoshujinInstanceIdentifier} == this)"))
            {
                if (this.Links != null)
                {
                    foreach (var link in this.Links)
                    {
                        if (link.Type == ChainType.None)
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

        ssb.AppendLine();*/
    }

    internal ScopingStringBuilder.IScope? ScopeLock(ScopingStringBuilder ssb)
    {
        return this.ObjectFlag.HasFlag(ValueLinkObjectFlag.EnableLock) ? ssb.ScopeBrace($"using ({ssb.FullObject}.Lock())") : null;
    }

    internal void GenerateGoshujin_Clear(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"public void Clear()"))
        using (var scopeThis = ssb.ScopeObject("this"))
        {
            if (this.Links != null)
            {
                var lockScope = this.ScopeLock(ssb);

                foreach (var link in this.Links)
                {
                    if (link.Type == ChainType.None)
                    {
                        continue;
                    }
                    else
                    {
                        ssb.AppendLine($"this.{link.ChainName}.Clear();");
                    }
                }

                lockScope?.Dispose();
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
            if (link.Type == ChainType.None)
            {
                continue;
            }
            else if (link.Type == ChainType.Ordered || link.Type == ChainType.ReverseOrdered || link.Type == ChainType.Unordered)
            {
                ssb.AppendLine($"public {link.Type.ChainTypeToName()}<{link.Target!.TypeObject!.FullName}, {this.LocalName}> {link.ChainName} {{ get; }}");
            }
            else
            {
                ssb.AppendLine($"public {link.Type.ChainTypeToName()}<{this.LocalName}> {link.ChainName} {{ get; }}");
            }
        }
    }

    internal void GenerateGoshujinInstance(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        var generateJournal = this.TinyhandAttribute?.Journaling == true;
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
                            foreach (var link in this.Links.Where(a => a.IsValidLink))
                            {
                                if (link.RemovedMethodName != null)
                                {
                                    using (var scopeRemove = ssb.ScopeBrace($"if (this.{goshujinInstance}.{link.ChainName}.Remove({ssb.FullObject}))"))
                                    {
                                        ssb.AppendLine($"this.{link.RemovedMethodName}();");
                                    }
                                }
                                else
                                {
                                    ssb.AppendLine($"this.{goshujinInstance}.{link.ChainName}.Remove({ssb.FullObject});");
                                }
                            }
                        }

                        if (this.PrimaryLink is not null && generateJournal)
                        {
                            this.CodeJournal2(ssb, this.PrimaryLink.Target);
                        }
                    }

                    ssb.AppendLine();
                    ssb.AppendLine($"this.{goshujinInstance} = value;");
                    if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateJournaling))
                    {
                        ssb.AppendLine($"this.Crystal = value?.Crystal;");
                    }

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

                        if (generateJournal)
                        {
                            this.CodeJournal2(ssb, null);
                        }
                    }
                }
            }
        }

        ssb.AppendLine();
        ssb.AppendLine($"private {this.ObjectAttribute!.GoshujinClass}? {goshujinInstance};");
        ssb.AppendLine();
    }

    internal bool ContainTinyhandObjectAttribute()
    {
        if (this.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject))
        {
            return true;
        }

        if (this.Children != null)
        {
            return this.Children.Any(a => a.ObjectFlag.HasFlag(ValueLinkObjectFlag.TinyhandObject));
        }

        return false;
    }
}
