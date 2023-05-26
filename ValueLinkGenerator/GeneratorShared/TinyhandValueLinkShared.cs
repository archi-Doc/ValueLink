// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Arc.Visceral;
using Tinyhand.Generator;
using ValueLink.Generator;

namespace ValueLinkGenerator;

internal static class TinyhandValueLinkShared
{
    public static string? ObjectToWriteKey(this ValueLinkObject obj)
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

    public static void ObjectToJournal(this ValueLinkObject obj, ScopingStringBuilder ssb)
    {
        var writeKey = ObjectToWriteKey(obj);
        if (writeKey is null)
        {
            return;
        }

        using (var journalScope = ssb.ScopeBrace("if (this.Crystal is not null && this.Crystal.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer))"))
        {
            // Custom locator
            using (var customScope = ssb.ScopeBrace($"if (this is Tinyhand.ITinyhandCustomJournal custom)"))
            {
                ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
            }

            // Key
            ssb.AppendLine("writer.Write_Key();");
            ssb.AppendLine(writeKey);

            // Value
            ssb.AppendLine("writer.Write_Value();");
            ssb.AppendLine($"writer.Write({ssb.FullObject});");
            ssb.AppendLine("this.Crystal.AddJournal(writer);");
        }
    }

    public static string? ObjectToReader(this ValueLinkObject obj)
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

        if (obj.AllAttributes.Any(x => x.FullName == "Tinyhand.TinyhandObject"))
        {// TinyhandObject
            return $"TinyhandSerializer.DeserializeObject<{obj.FullName}>(ref reader)";
        }

        return $"TinyhandSerializer.Deserialize<{obj.FullName}>(ref reader)";
    }
}
