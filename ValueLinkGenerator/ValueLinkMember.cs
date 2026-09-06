// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tinyhand.Generator;
using TinyhandGenerator;

namespace ValueLink.Generator;

/// <summary>
/// Describes and emits a record member exposed through a generated writer.
/// </summary>
public class Member
{
    public static Member? Create(ValueLinkObject parent, ValueLinkObject obj, Linkage? linkage, bool journaling)
    {
        obj.GetRawInformation(out var symbol, out _, out _);
        if (symbol is IPropertySymbol property && !property.ExplicitInterfaceImplementations.IsEmpty)
        {
            // Explicit interface properties are not accessible through the record instance.
            return null;
        }

        if (obj.SimpleName.Length == 0/* || !char.IsLower(obj.SimpleName[0])*/)
        {
            return null;
        }

        var name = obj.SimpleName;
        string generatedName;
        if (obj.KeyAttribute is not null &&
            !string.IsNullOrEmpty(obj.KeyAttribute.AddProperty))
        {
            generatedName = obj.KeyAttribute.AddProperty;
        }
        else if (char.IsLower(name[0]))
        {
            generatedName = char.ToUpperInvariant(name[0]) + name.Substring(1);
            if (parent.AllMembers.Any(x => x.SimpleName == generatedName))
            {
                return null;
            }
        }
        else
        {
            generatedName = name;
        }

        var member = new Member(obj, linkage, journaling, generatedName)
        {
            CanAccessOriginal = HasPlainStorage(symbol),
        };
        return member;
    }

    private static bool HasPlainStorage(ISymbol? symbol)
    {
        if (symbol is IFieldSymbol)
        {
            return true;
        }

        if (symbol is IPropertySymbol { IsPartialDefinition: false, IsVirtual: false, IsAbstract: false, IsOverride: false } property)
        {
            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is PropertyDeclarationSyntax { AccessorList: { } list })
                {
                    foreach (var accessor in list.Accessors)
                    {
                        if (accessor.Body is not null || accessor.ExpressionBody is not null)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        return false;
    }

    public Member(ValueLinkObject obj, Linkage? linkage, bool journaling, string generatedName)
    {
        this.Object = obj;
        this.Linkage = linkage;

        this.GeneratedName = generatedName;

        // this.ChangedName = this.Linkage is null ? null : this.Object.SimpleName + "Changed";
        if (this.Linkage is not null ||
            (journaling && obj.AllAttributes.Any(x => x.FullName == KeyAttributeMock.FullName)))
        {
            if (!this.Object.IsGetterOnlyProperty)
            {
                this.ChangedName = this.Object.SimpleName + "Changed";
            }
        }

        if (obj.AllAttributes.FirstOrDefault(x => x.FullName == MaxLengthAttributeMock.FullName) is { } objectAttribute)
        {
            try
            {
                this.MaxLengthAttribute = MaxLengthAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
            }
            catch
            {
            }
        }
    }

    public ValueLinkObject Object { get; private set; }

    public Linkage? Linkage { get; private set; }

    public Location Location => this.Object.Location;

    public string GeneratedName { get; private set; }

    public string? ChangedName { get; private set; }

    public MaxLengthAttributeMock? MaxLengthAttribute { get; private set; }

    private bool CanAccessOriginal { get; set; }

    private string ReadInstance => this.CanAccessOriginal ? "(this.instance ?? this.original)" : "this.Instance";

    public void GenerateReaderProperty(ScopingStringBuilder ssb, string disposedIdentifier)
    {
        ssb.AppendLine($"public {this.Object.TypeObjectWithNullable?.FullNameWithNullable} {this.GeneratedName} {{ get {{ ObjectDisposedException.ThrowIf(this.{disposedIdentifier}, this); return {this.ReadInstance}.{this.Object.SimpleName}; }} }}");
    }

    public void GenerateWriterProperty(ScopingStringBuilder ssb, string disposedIdentifier)
    {
        using (var scopeProperty = ssb.ScopeBrace($"public {this.Object.TypeObjectWithNullable?.FullNameWithNullable} {this.GeneratedName}"))
        {
            ssb.AppendLine($"get {{ ObjectDisposedException.ThrowIf(this.{disposedIdentifier}, this); return {this.ReadInstance}.{this.Object.SimpleName}; }}");
            using (var scopeSetter = ssb.ScopeBrace($"set"))
            {
                ssb.AppendLine($"ObjectDisposedException.ThrowIf(this.{disposedIdentifier}, this);");
                if (this.MaxLengthAttribute is not null)
                {
                    JournalShared.GenerateValue_MaxLength(ssb, this.Object, this.MaxLengthAttribute);
                }

                /*if (this.Object.TypeObject is { } typeObject &&
                    this.Object.ContainingObject?.TinyhandAttribute?.Tree == true)
                {
                    if (typeObject.TinyhandAttribute?.Tree == true ||
                        typeObject.ObjectFlag.HasFlag(ValueLinkObjectFlag.GenerateJournal) == true ||
                        typeObject.Kind == VisceralObjectKind.Error)
                    {
                        ssb.AppendLine($"if (value is {TinyhandBody.IStructuralObject} obj) obj.SetupStructure(this.Instance);");
                    }
                }*/

                if (this.CanAccessOriginal)
                {
                    var type = this.Object.TypeObject!.FullName;
                    this.Object.TypeObject.GetRawInformation(out var typeSymbol, out _, out _);
                    var valueEquals = $"EqualityComparer<{this.Object.TypeObjectWithNullable?.FullNameWithNullable}>.Default.Equals((this.instance ?? this.original).{this.Object.SimpleName}, value)";
                    var referenceEquals = $"ReferenceEquals((this.instance ?? this.original).{this.Object.SimpleName}, value)";
                    var comparison = typeSymbol is ITypeSymbol { IsValueType: true } ? valueEquals
                        : typeSymbol is ITypeSymbol { IsReferenceType: true } ? referenceEquals
                        : $"typeof({type}).IsValueType ? {valueEquals} : {referenceEquals}";
                    ssb.AppendLine($"if ({comparison}) return;");
                }

                ssb.AppendLine($"this.Instance.{this.Object.SimpleName} = value;");
                if (this.ChangedName is not null)
                {
                    ssb.AppendLine($"this.{this.ChangedName} = true;");
                }
            }
        }
    }

    /*public void GenerateWriterProperty2(ScopingStringBuilder ssb)
    {
        using (var scopeProperty = ssb.ScopeBrace($"public {(this.NewKeyword ? "new " : string.Empty)}{this.Object.TypeObject?.FullName} {this.GeneratedName}"))
        {
            ssb.AppendLine($"get => {this.AccessorName}.{this.Object.SimpleName};");
            using (var scopeSetter = ssb.ScopeBrace($"set"))
            {
                ssb.AppendLine($"{this.AccessorName}.{this.Object.SimpleName} = value;");
                if (this.ChangedName is not null)
                {
                    ssb.AppendLine($"this.{this.ChangedName} = true;");
                }
            }
        }
    }*/
}
