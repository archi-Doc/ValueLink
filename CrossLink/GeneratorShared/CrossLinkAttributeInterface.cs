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
    public class CrossLinkAttribute : Attribute
    {
        public LinkType Type { get; private set; }

        public string? Name { get; private set; }

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
