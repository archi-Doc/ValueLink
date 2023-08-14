// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Generator;
using TinyhandGenerator;

namespace ValueLink.Generator;

public class Member
{
    public static Member? Create(ValueLinkObject obj, Linkage? linkage, bool journaling)
    {
        if (obj.SimpleName.Length == 0/* || !char.IsLower(obj.SimpleName[0])*/)
        {
            return null;
        }

        var member = new Member(obj, linkage, journaling);
        return member;
    }

    public Member(ValueLinkObject obj, Linkage? linkage, bool journaling)
    {
        this.Object = obj;
        this.Linkage = linkage;

        var name = obj.SimpleName;
        if (char.IsLower(obj.SimpleName[0]))
        {
            this.GeneratedName = name[0].ToString().ToUpper() + name.Substring(1);
        }
        else
        {
            this.GeneratedName = name;
        }

        // this.ChangedName = this.Linkage is null ? null : this.Object.SimpleName + "Changed";
        if (this.Linkage is not null ||
            (journaling && obj.AllAttributes.Any(x => x.FullName == KeyAttributeMock.FullName)))
        {
            this.ChangedName = this.Object.SimpleName + "Changed";
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

    public void GenerateReaderProperty(ScopingStringBuilder ssb)
    {
        ssb.AppendLine($"public {this.Object.TypeObject?.FullName} {this.GeneratedName} => this.Instance.{this.Object.SimpleName};");
    }

    public void GenerateWriterProperty(ScopingStringBuilder ssb)
    {
        using (var scopeProperty = ssb.ScopeBrace($"public {this.Object.TypeObjectWithNullable?.FullNameWithNullable} {this.GeneratedName}"))
        {
            ssb.AppendLine($"get => this.Instance.{this.Object.SimpleName};");
            using (var scopeSetter = ssb.ScopeBrace($"set"))
            {
                if (this.MaxLengthAttribute is not null)
                {
                    JournalShared.GenerateValue_MaxLength(ssb, this.Object, this.MaxLengthAttribute);
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
