// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink
{
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

    public static class AttributeHelper
    {
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

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LinkAttributeMock : Attribute
    {
        public static readonly string SimpleName = "Link";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "ValueLink." + StandardName;

        /// <summary>
        /// Gets or sets a value indicating the type of object linkage.
        /// </summary>
        public ChainType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the link is a primary link that is guaranteed to holds all objects in the collection [the default is false].
        /// </summary>
        public bool Primary { get; set; } = false;

        /// <summary>
        /// Gets or sets a string value which represents the name used for the linkage interface.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether or not to link the object automatically when the goshujin is set or changed [the default is true].
        /// </summary>
        public bool AutoLink { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not to invoke PropertyChanged event when the value has changed [the default is false].
        /// </summary>
        public bool AutoNotify { get; set; } = false;

        /// <summary>
        /// Gets or sets a string value which represents the target member(property or field) name of the linkage.<br/>
        /// Only LinkAttribute annotated to constructor is supported.
        /// </summary>
        public string TargetMember { get; set; } = string.Empty;

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

        public bool UseModuleInitializer { get; set; } = true;

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

            val = AttributeHelper.GetValue(-1, nameof(UseModuleInitializer), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.UseModuleInitializer = (bool)val;
            }

            return attribute;
        }
    }
}
