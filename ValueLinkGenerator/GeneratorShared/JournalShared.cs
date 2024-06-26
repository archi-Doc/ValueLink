﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand.Generator;
using ValueLink.Generator;

namespace TinyhandGenerator;

internal static class JournalShared
{
    public static void GenerateValue_MaxLength(ScopingStringBuilder ssb, ValueLinkObject x, MaxLengthAttributeMock attribute)
    {
        if (x.TypeObject is not { } typeObject)
        {
            return;
        }

        if (typeObject.FullName == "string")
        {// string
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Length > {attribute.MaxLength})"))
                {// text = text.Substring(0, MaxLength);
                    ssb.AppendLine($"value = value.Substring(0, {attribute.MaxLength});");
                }
            }
        }
        else if (typeObject.Array_Rank == 1)
        {// T[]
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Length > {attribute.MaxLength})"))
                {// array = array[..MaxLength];
                    ssb.AppendLine($"value = value[..{attribute.MaxLength}];");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Array_Element?.FullName == "string")
                {// string[]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"value[i] = value[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Array_Element?.Array_Rank == 1)
                {// T[][]
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Length; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"value[i] = value[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
        else if (typeObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
            typeObject.OriginalDefinition is { } baseObject &&
            baseObject.FullName == "System.Collections.Generic.List<T>" &&
            typeObject.Generics_Arguments.Length == 1)
        {// List<T>
            if (attribute.MaxLength >= 0)
            {
                using (var scopeIf = ssb.ScopeBrace($"if (value.Count > {attribute.MaxLength})"))
                {// list = list.GetRange(0, MaxLength);
                    ssb.AppendLine($"value = value.GetRange(0, {attribute.MaxLength});");
                }
            }

            if (attribute.MaxChildLength >= 0)
            {
                if (typeObject.Generics_Arguments[0].FullName == "string")
                {// List<string>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {// text = text.Substring(0, MaxLength);
                            ssb.AppendLine($"value[i] = value[i].Substring(0, {attribute.MaxChildLength});");
                        }
                    }
                }
                else if (typeObject.Generics_Arguments[0].Array_Rank == 1)
                {// List<T[]>
                    using (var scopeFor = ssb.ScopeBrace($"for (var i = 0; i < value.Count; i++)"))
                    {
                        using (var scopeIf = ssb.ScopeBrace($"if (value[i].Length > {attribute.MaxChildLength})"))
                        {
                            ssb.AppendLine($"value[i] = value[i][..{attribute.MaxChildLength}];");
                        }
                    }
                }
            }
        }
    }

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

    public static void CodeJournal2(this ValueLinkObject obj, ScopingStringBuilder ssb, ValueLinkObject? remove, InterfaceImplementation customJournal)
    {
        using (var journalScope = ssb.ScopeBrace($"if (writeJournal && (({TinyhandBody.IStructualObject}){ssb.FullObject}).TryGetJournalWriter(out var root, out var writer, false))"))
        {
            // Custom locator
            if (customJournal == InterfaceImplementation.Unknown)
            {
                using (var customScope = ssb.ScopeBrace($"if ({ssb.FullObject} is {TinyhandBody.ITinyhandCustomJournal} custom)"))
                {
                    ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
                }
            }
            else if (customJournal == InterfaceImplementation.Implemented)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandCustomJournal}){ssb.FullObject}).WriteCustomLocator(ref writer);");
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
                ssb.AppendLine("if (erase) writer.Write(JournalRecord.RemoveAndErase);");
                ssb.AppendLine("else writer.Write(JournalRecord.Remove);");
                ssb.AppendLine(writeRemove);
            }

            ssb.AppendLine($"root.AddJournal(ref writer);");
        }
    }

    public static void CodeJournal3(this ValueLinkObject obj, ScopingStringBuilder ssb, InterfaceImplementation customJournal)
    {
        if (obj.Members is null)
        {
            return;
        }

        using (var journalScope = ssb.ScopeBrace($"if ((({TinyhandBody.IStructualObject})this.original).TryGetJournalWriter(out var root, out var writer, true))"))
        {
            // Custom locator
            if (customJournal == InterfaceImplementation.Unknown)
            {
                using (var customScope = ssb.ScopeBrace($"if (this.instance is {TinyhandBody.ITinyhandCustomJournal} custom)"))
                {
                    ssb.AppendLine("custom.WriteCustomLocator(ref writer);");
                }
            }
            else if (customJournal == InterfaceImplementation.Implemented)
            {
                ssb.AppendLine($"(({TinyhandBody.ITinyhandCustomJournal})this.instance).WriteCustomLocator(ref writer);");
            }

            // Locator
            /*if (obj.UniqueLink is not null &&
                obj.UniqueLink.TypeObject.CodeWriter($"this.original.{obj.UniqueLink.TargetName}") is { } writeLocator)
            {// this.instance
                ssb.AppendLine("writer.Write_Locator();");
                ssb.AppendLine(writeLocator);
            }*/

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

            ssb.AppendLine("root.AddJournal(ref writer);");
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
