// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Generator;
using ValueLink.Generator;

namespace TinyhandGenerator;

internal static class JournalShared
{
    public static bool IsSupportedPrimitive(this ValueLinkObject obj)
        => obj.FullName switch
        {
            "bool" => true,
            "byte" => true,
            "sbyte" => true,
            "ushort" => true,
            "short" => true,
            "uint" => true,
            "int" => true,
            "ulong" => true,
            "long" => true,

            "char" => true,
            "string" => true,
            "float" => true,
            "double" => true,

            _ => false,
        };

    public static string? CodeWriteKey(this ValueLinkObject obj)
    {
        int intKey = -1;
        string? stringKey = null;

        foreach (var x in obj.AllAttributes)
        {
            if (x.FullName == KeyAttributeMock.FullName)
            {// KeyAttribute
                var val = x.ConstructorArguments[0];
                if (val is int i)
                {
                    intKey = i;
                    break;
                }
                else if (val is string s)
                {
                    stringKey = s;
                    break;
                }
            }
            else if (x.FullName == KeyAsNameAttributeMock.FullName)
            {// KeyAsNameAttribute
                stringKey = obj.SimpleName;
                break;
            }
        }

        if (intKey >= 0)
        {
            return $"writer.Write({intKey.ToString()});";
        }
        else if (stringKey is not null)
        {
            return $"writer.WriteString(\"{stringKey}\"u8);";
        }
        else
        {
            return null;
        }
    }

    public static void CodeJournal(this ValueLinkObject obj, ScopingStringBuilder ssb, ValueLinkObject? locator)
    {
        using (var journalScope = ssb.ScopeBrace("if (this.Crystal is not null && this.Crystal.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer))"))
        {
            // Custom locator
            using (var customScope = ssb.ScopeBrace($"if (this is Tinyhand.ITinyhandCustomJournal custom)"))
            {
                ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
            }

            // Locator
            if (locator is not null &&
                obj.CodeWriter($"this.{locator.SimpleName}") is { } writeLocator)
            {
                ssb.AppendLine("writer.Write_Locator();");
                ssb.AppendLine(writeLocator);
            }

            // Key
            var writeKey = obj.CodeWriteKey();
            if (writeKey is not null)
            {
                ssb.AppendLine("writer.Write_Key();");
                ssb.AppendLine(writeKey);
            }

            // Value
            var writeValue = obj.CodeWriter(ssb.FullObject);
            if (writeValue is not null)
            {
                ssb.AppendLine("writer.Write_Value();");
                ssb.AppendLine(writeValue);
            }

            ssb.AppendLine("this.Crystal.AddJournal(writer);");
        }
    }

    public static void CodeJournal2(this ValueLinkObject obj, ScopingStringBuilder ssb, ValueLinkObject? remove)
    {
        using (var journalScope = ssb.ScopeBrace($"if ({ssb.FullObject}.Crystal is not null && {ssb.FullObject}.Crystal.TryGetJournalWriter(JournalType.Record, {ssb.FullObject}.CurrentPlane, out var writer))"))
        {
            // Custom locator
            using (var customScope = ssb.ScopeBrace($"if ({ssb.FullObject} is Tinyhand.ITinyhandCustomJournal custom)"))
            {
                ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
            }

            // Add
            if (remove is null &&
                obj.CodeWriter(ssb.FullObject) is { } writeAdd)
            {
                ssb.AppendLine("writer.Write_Add();");
                ssb.AppendLine(writeAdd);
            }

            // Remove
            if (remove is not null &&
                remove.CodeWriter($"{ssb.FullObject}.{remove.SimpleName}") is { } writeRemove)
            {
                ssb.AppendLine("writer.Write_Remove();");
                ssb.AppendLine(writeRemove);
            }

            ssb.AppendLine($"{ssb.FullObject}.Crystal.AddJournal(writer);");
        }
    }

    public static void CodeJournal3(this ValueLinkObject obj, ScopingStringBuilder ssb)
    {
        if (obj.Members is null)
        {
            return;
        }

        using (var journalScope = ssb.ScopeBrace("if (this.instance.Crystal is not null && this.instance.Crystal.TryGetJournalWriter(JournalType.Record, this.instance.CurrentPlane, out var writer))"))
        {
            // Custom locator
            using (var customScope = ssb.ScopeBrace($"if (this.instance is Tinyhand.ITinyhandCustomJournal custom)"))
            {
                ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
            }

            // Locator
            if (obj.UniqueLink is not null &&
                obj.UniqueLink.TypeObject.CodeWriter($"this.instance.{obj.UniqueLink.TargetName}") is { } writeLocator)
            {
                ssb.AppendLine("writer.Write_Locator();");
                ssb.AppendLine(writeLocator);
            }

            foreach (var x in obj.Members)
            {
                if (x.ChangedName is not null)
                {
                    var memberObject = x.Object;
                    using (var scopeChanged = ssb.ScopeBrace($"if (this.{x.ChangedName})"))
                    {
                        // Key
                        var writeKey = memberObject.CodeWriteKey();
                        if (writeKey is not null)
                        {
                            ssb.AppendLine("writer.Write_Key();");
                            ssb.AppendLine(writeKey);
                        }

                        // Value
                        var writeValue = memberObject.CodeWriter($"this.instance.{memberObject.SimpleName}");
                        if (writeValue is not null)
                        {
                            ssb.AppendLine("writer.Write_Value();");
                            ssb.AppendLine(writeValue);
                        }
                    }
                }
            }

            ssb.AppendLine("this.instance.Crystal.AddJournal(writer);");
        }
    }

    public static string? CodeReader(this ValueLinkObject obj)
    {
        var coder = obj.FullName switch
        {
            "bool" => "reader.ReadBoolean()",
            "byte" => "reader.ReadUInt8()",
            "sbyte" => "reader.ReadInt8()",
            "ushort" => "reader.ReadUInt16()",
            "short" => "reader.ReadInt16()",
            "uint" => "reader.ReadUInt32()",
            "int" => "reader.ReadInt32()",
            "ulong" => "reader.ReadUInt64()",
            "long" => "reader.ReadInt64()",

            "char" => "reader.ReadChar()",
            "string" => "reader.ReadString()",
            "float" => "reader.ReadSingle()",
            "double" => "reader.ReadDouble()",

            _ => null,
        };

        if (coder is not null)
        {
            return coder;
        }

        if (obj.AllAttributes.Any(x => x.FullName == TinyhandObjectAttributeMock.FullName))
        {// TinyhandObject
            return $"TinyhandSerializer.DeserializeAndReconstructObject<{obj.FullName}>(ref reader)";
        }

        return $"TinyhandSerializer.Deserialize<{obj.FullName}>(ref reader)";
    }

    public static string? CodeWriter(this ValueLinkObject obj, string valueString)
    {
        var name = obj.TypeObject?.FullName;
        if (name is null)
        {
            return null;
        }
        else if (name == "bool" || name == "byte" || name == "sbyte" || name == "ushort" ||
            name == "short" || name == "uint" || name == "int" || name == "ulong" ||
            name == "long" || name == "char" || name == "string" || name == "float" ||
            name == "double")
        {
            return $"writer.Write({valueString});";
        }

        if (obj.AllAttributes.Any(x => x.FullName == TinyhandObjectAttributeMock.FullName))
        {// TinyhandObject
            return $"TinyhandSerializer.SerializeObject(ref writer, {valueString});";
        }

        return $"TinyhandSerializer.Serialize(ref writer, {valueString});";
    }
}
