// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace CrossLink
{
    public enum ChainType
    {
        /// <summary>
        /// No link. In case you want to use AutoNotify only.
        /// </summary>
        None,

        /// <summary>
        /// Represents a list of objects (<see cref="ListChain{T}"/>).
        /// <br/>Structure: Array.
        /// </summary>
        List,

        /// <summary>
        /// Represents a doubly linked list of objects (<see cref="LinkedListChain{T}"/>).
        /// <br/>Structure: Doubly linked list.
        /// </summary>
        LinkedList,

        /// <summary>
        /// Represents a stack list (<see cref="StackListChain{T}"/>).
        /// <br/>Structure: Doubly linked list.
        /// </summary>
        StackList,

        /// <summary>
        /// Represents a queue list (<see cref="QueueListChain{T}"/>).
        /// <br/>Structure: Doubly linked list.
        /// </summary>
        QueueList,

        /// <summary>
        /// Represents a collection of sorted objects (<see cref="OrderedChain{TKey, TValue}"/>).
        /// <br/>Structure: Red-Black Tree + Linked List.
        /// </summary>
        Ordered,

        /// <summary>
        /// Represents a collection of objects sorted in reverse order (<see cref="OrderedChain{TKey, TValue}"/>).
        /// <br/>Structure: Red-Black Tree + Linked List.
        /// </summary>
        ReverseOrdered,

        /// <summary>
        /// Represents a collection of objects stored in a hash table (<see cref="UnorderedChain{TKey, TValue}"/>).
        /// <br/>Structure: Hash table.
        /// </summary>
        Unordered,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CrossLinkObjectAttribute : Attribute
    {
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

        public CrossLinkObjectAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LinkAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating the type of object linkage (chain).
        /// </summary>
        public ChainType Type { get; set; } = ChainType.None;

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
