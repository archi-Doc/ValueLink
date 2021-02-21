// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace CrossLink
{
    public enum LinkType
    {
        /// <summary>
        /// Represents a doubly linked list.
        /// </summary>
        LinkedList,

        /// <summary>
        /// Represents a collection of sorted objects.
        /// </summary>
        SortedList,
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CrossLinkAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating the type of object linkage.
        /// </summary>
        public LinkType Type { get; set; }

        /// <summary>
        /// Gets or sets a string value which represents the name used for the linkage interface.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public CrossLinkAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CrossLinkGeneratorOptionAttribute : Attribute
    {
        public bool AttachDebugger { get; set; } = false;

        public bool GenerateToFile { get; set; } = false;

        public string? CustomNamespace { get; set; }

        public CrossLinkGeneratorOptionAttribute()
        {
        }
    }
}
