// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace CrossLink
{
    public enum LinkType
    {
        /// <summary>
        /// No link. In case you want to use AutoNotify only.
        /// </summary>
        None,

        /// <summary>
        /// Represents a list of objects (<see cref="ListChain{T}"/>).
        /// </summary>
        List,

        /// <summary>
        /// Represents a doubly linked list (<see cref="LinkedListChain{T}"/>).
        /// </summary>
        LinkedList,

        /// <summary>
        /// Represents a stack list (<see cref="StackListChain{T}"/>).
        /// </summary>
        StackList,

        /// <summary>
        /// Represents a queue list (<see cref="QueueListChain{T}"/>).
        /// </summary>
        QueueList,

        /// <summary>
        /// Represents a collection of sorted objects (<see cref="OrderedChain{TKey, TValue}"/>).
        /// </summary>
        Ordered,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CrossLinkObjectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a string value which represents the class name of Goshujin (Owner class) [Default value is "GoshujinClass"].
        /// </summary>
        public string GoshujinClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a string value which represents the instance name of Goshujin (Owner class) [Default value is "Goshujin"].
        /// </summary>
        public string GoshujinInstance { get; set; } = string.Empty;

        public CrossLinkObjectAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LinkAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating the type of object linkage.
        /// </summary>
        public LinkType Type { get; set; } = LinkType.None;

        /// <summary>
        /// Gets or sets a string value which represents the name used for the linkage interface.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether or not to create a link automatically [Default value is true].
        /// </summary>
        public bool AutoLink { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not to implement the INotifyPropertyChanged pattern. The class must inherit from <see cref="BindableBase" /> [Default value is false].
        /// </summary>
        public bool AutoNotify { get; set; } = false;

        public LinkAttribute()
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
