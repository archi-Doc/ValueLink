// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace CrossLink
{
    public enum LinkType
    {
        None,
        List,
        LinkedList,
        StackList,
        QueueList,
        Ordered,
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
    public sealed class CrossLinkObjectAttributeMock : Attribute
    {
        public static readonly string SimpleName = "CrossLinkObject";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "CrossLink." + StandardName;

        /// <summary>
        /// Gets or sets a string value which represents the class name of Goshujin (Owner class) [Default value is "GoshujinClass"].
        /// </summary>
        public string GoshujinClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a string value which represents the instance name of Goshujin (Owner class) [Default value is "Goshujin"].
        /// </summary>
        public string GoshujinInstance { get; set; } = string.Empty;

        public CrossLinkObjectAttributeMock()
        {
        }

        public static CrossLinkObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new CrossLinkObjectAttributeMock();
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

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LinkAttributeMock : Attribute
    {
        public static readonly string SimpleName = "Link";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "CrossLink." + StandardName;

        /// <summary>
        /// Gets or sets a value indicating the type of object linkage.
        /// </summary>
        public LinkType Type { get; set; }

        /// <summary>
        /// Gets or sets a string value which represents the name used for the linkage interface.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether or not to create a link automatically [Default value is true].
        /// </summary>
        public bool AutoLink { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not to implement the INotifyPropertyChanged pattern. The class must inherit from CrossLink.BindableBase. [Default value is false].
        /// </summary>
        public bool AutoNotify { get; set; } = false;

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
                attribute.Type = (LinkType)val;
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

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CrossLinkGeneratorOptionAttributeMock : Attribute
    {
        public static readonly string SimpleName = "CrossLinkGeneratorOption";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "CrossLink." + StandardName;

        public bool AttachDebugger { get; set; } = false;

        public bool GenerateToFile { get; set; } = false;

        public string? CustomNamespace { get; set; }

        public CrossLinkGeneratorOptionAttributeMock()
        {
        }

        public static CrossLinkGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new CrossLinkGeneratorOptionAttributeMock();
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
}
