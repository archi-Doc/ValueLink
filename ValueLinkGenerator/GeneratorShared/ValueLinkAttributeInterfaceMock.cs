// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink;

// To add chain : Add ChainType, GeneratorHelper.ChainTypeToName
// If necessary: Generate_AddLink(), GenerateGoshujin_Chain()
public enum ChainType
{
    None,
    List,
    LinkedList,
    StackList,
    QueueList,
    Ordered,
    ReverseOrdered,
    Unordered,
    Observable,
}

public enum IsolationLevel
{
    None,
    Serializable,
    RepeatablePrimitives,
}

public enum ValueLinkAccessibility
{
    PublicGetter,
    Public,
    Inherit,
}

public static class AttributeHelper
{
    public static bool IsLocatable(this ChainType chainType) => chainType switch
    {
        ChainType.Ordered => true,
        ChainType.ReverseOrdered => true,
        ChainType.Unordered => true,
        _ => false,
    };

    public static object? GetValue(int constructorIndex, string? name, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        if (constructorIndex >= 0 && constructorIndex < constructorArguments.Length)
        {// Constructor Argument.
            return constructorArguments[constructorIndex];
        }
        else if (name != null)
        {// Named Argument.
            var pair = namedArguments.FirstOrDefault(x => x.Key == name);
            if (pair.Equals(default(KeyValuePair<string, object?>)))
            {
                return null;
            }

            return pair.Value;
        }
        else
        {
            return null;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkObjectAttributeMock : Attribute
{
    public static readonly string SimpleName = "ValueLinkObject";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "ValueLink." + StandardName;

    /// <summary>
    /// Gets or sets a string value which represents the class name of Goshujin (Owner class) [the default is "GoshujinClass"].
    /// </summary>
    public string GoshujinClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a string value which represents the instance name of Goshujin (Owner class) [the default is "Goshujin"].
    /// </summary>
    public string GoshujinInstance { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a string value which represents the explicit name of INotifyPropertyChanged.PropertyChanged event [the default is "PropertyChanged"].
    /// </summary>
    public string ExplicitPropertyChanged { get; set; } = string.Empty;

    public IsolationLevel Isolation { get; set; } = IsolationLevel.None;

    public ValueLinkObjectAttributeMock()
    {
    }

    public static ValueLinkObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new ValueLinkObjectAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(GoshujinClass), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GoshujinClass = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(GoshujinInstance), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GoshujinInstance = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(ExplicitPropertyChanged), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ExplicitPropertyChanged = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(Isolation), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Isolation = (IsolationLevel)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class LinkAttributeMock : Attribute
{
    public static readonly string SimpleName = "Link";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "ValueLink." + StandardName;

    public ChainType Type { get; set; }

    public bool Primary { get; set; } = false;

    public string Name { get; set; } = string.Empty;

    public bool AutoLink { get; set; } = true;

    public bool AutoNotify { get; set; } = false;

    public string TargetMember { get; set; } = string.Empty;

    public ValueLinkAccessibility Accessibility { get; set; } = ValueLinkAccessibility.PublicGetter;

    public bool AddValue { get; set; } = true;

    public LinkAttributeMock()
    {
    }

    public static LinkAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new LinkAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(Type), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Type = (ChainType)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(Primary), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Primary = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(Name), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Name = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(AutoLink), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AutoLink = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(AutoNotify), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AutoNotify = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(TargetMember), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.TargetMember = (string)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(Accessibility), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Accessibility = (ValueLinkAccessibility)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(AddValue), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AddValue = (bool)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkGeneratorOptionAttributeMock : Attribute
{
    public static readonly string SimpleName = "ValueLinkGeneratorOption";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "ValueLink." + StandardName;

    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public ValueLinkGeneratorOptionAttributeMock()
    {
    }

    public static ValueLinkGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new ValueLinkGeneratorOptionAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AttachDebugger = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GenerateToFile = (bool)val;
        }

        val = AttributeHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.CustomNamespace = (string)val;
        }

        return attribute;
    }
}
