// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using Arc.Collections;

namespace ValueLink;

/// <summary>
/// Selects the generated owner's concurrency model.
/// </summary>
public enum IsolationLevel
{
    /// <summary>
    /// Provides no automatic synchronization.
    /// </summary>
    None,

    /// <summary>
    /// Provides an owner lock for synchronized access and storage operations.
    /// </summary>
    Serializable,

    /// <summary>
    /// Delegates data acquisition to an <see cref="IDataLocker{TData}"/> implementation.
    /// Repeated reads may observe different data.
    /// </summary>
    ReadCommitted,

    /// <summary>
    /// Publishes record copies through exclusive writers. Requires a record class and a unique key.
    /// Mutable reference-type members must be copied explicitly.
    /// </summary>
    RepeatableRead,
}

/// <summary>
/// Selects the collection used to index or order owned objects.
/// </summary>
public enum ChainType
{
    /// <summary>
    /// Creates no chain; useful for generated values and notifications alone.
    /// </summary>
    None,

    /// <summary>
    /// Uses an indexed <see cref="ListChain{T}"/> whose insertion and removal can reorder objects.
    /// </summary>
    List,

    /// <summary>
    /// Uses a doubly linked <see cref="LinkedListChain{T}"/> with direct navigation and removal.
    /// </summary>
    LinkedList,

    /// <summary>
    /// Uses a last-in, first-out <see cref="StackListChain{T}"/>.
    /// </summary>
    StackList,

    /// <summary>
    /// Uses a first-in, first-out <see cref="QueueListChain{T}"/>.
    /// </summary>
    QueueList,

    /// <summary>
    /// Uses an ascending <see cref="OrderedChain{TKey, TValue}"/> that permits duplicate keys.
    /// </summary>
    Ordered,

    /// <summary>
    /// Uses an <see cref="OrderedChain{TKey, TValue}"/> with reversed comparison and traversal.
    /// </summary>
    ReverseOrdered,

    /// <summary>
    /// Uses a hash-based <see cref="UnorderedChain{TKey, TValue}"/> that permits duplicate keys.
    /// </summary>
    Unordered,

    /// <summary>
    /// Uses an indexed <see cref="ObservableChain{T}"/> with collection-change notifications.
    /// </summary>
    Observable,

    /// <summary>
    /// Uses a bounded <see cref="SlidingListChain{T}"/> with manual membership and logical positions.
    /// </summary>
    SlidingList,
}

/// <summary>
/// Specifies the accessibility of generated Value/Link members.
/// </summary>
public enum ValueLinkAccessibility
{
    /// <summary>
    /// Gives value properties a public getter and inherited setter access; links remain publicly accessible.
    /// </summary>
    PublicGetter,

    /// <summary>
    /// Makes generated value and link members public.
    /// </summary>
    Public,

    /// <summary>
    /// Makes generated value and link members protected.
    /// </summary>
    Protected,

    /// <summary>
    /// Makes generated value and link members private.
    /// </summary>
    Private,

    /// <summary>
    /// Uses the target member's accessibility for generated value and link members.
    /// </summary>
    Inherit,
}

/// <summary>
/// Enables owner and chain generation for a partial type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkObjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the generated owner class name; an empty value uses GoshujinClass.
    /// </summary>
    public string GoshujinClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated owner property name; an empty value uses Goshujin.
    /// </summary>
    public string GoshujinInstance { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event name used for property notifications; an empty value uses PropertyChanged.
    /// </summary>
    public string ExplicitPropertyChanged { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner's isolation mode. The default is None.
    /// </summary>
    public IsolationLevel Isolation { get; set; } = IsolationLevel.None;

    /// <summary>
    /// Gets or sets a value indicating whether the owner property is internal and links default to private access without generated value properties.
    /// </summary>
    public bool Restricted { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to generate Tinyhand-based difference synchronization. Requires a unique value-type key and None or Serializable isolation.
    /// </summary>
    public bool Integrality { get; set; } = false;

    public ValueLinkObjectAttribute()
    {
    }
}

/// <summary>
/// Configures a chain, its per-object link, and optional value notifications.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class LinkAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the chain type. The default is None.
    /// </summary>
    public ChainType Type { get; set; } = ChainType.None;

    /// <summary>
    /// Gets or sets a value indicating whether this chain supplies owner enumeration and serialization order. Keep every owned object in the primary chain.
    /// </summary>
    public bool Primary { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this link identifies objects for isolation, journaling, and synchronization. Raw chain operations still allow duplicate keys.
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Gets or sets the prefix for generated chain and link members. An empty value uses the target member name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether assigning an owner automatically adds this link. The default is true; sliding chains always require manual insertion.
    /// </summary>
    /// <remarks>
    /// Generated value changes can update or add a link even when AutoLink is false.
    /// </remarks>
    public bool AutoLink { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether generated value changes raise PropertyChanged. The default is false.
    /// </summary>
    public bool AutoNotify { get; set; } = false;

    /// <summary>
    /// Gets or sets the field or property name indexed by a constructor-level attribute.
    /// </summary>
    public string TargetMember { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an existing chain name to share. Pass the correct link by reference when manipulating shared entries.
    /// </summary>
    public string UnsafeTargetChain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets access to generated value and link members. The default is PublicGetter.
    /// </summary>
    public ValueLinkAccessibility Accessibility { get; set; } = ValueLinkAccessibility.PublicGetter;

    /// <summary>
    /// Gets or sets a value indicating whether to generate a value property that updates links. The default is false; partial properties generate their own accessors.
    /// </summary>
    public bool AddValue { get; set; } = false;

    public LinkAttribute()
    {
    }
}

/// <summary>
/// Configures source output and initialization for the ValueLink generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkGeneratorOptionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether debugger attachment is requested. Reserved; the current generator does not attach a debugger.
    /// </summary>
    public bool AttachDebugger { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to write generated files to an existing Generated folder beside the attributed source file.
    /// </summary>
    public bool GenerateToFile { get; set; } = false;

    /// <summary>
    /// Gets or sets the namespace of the generated module initializer. Model namespaces are unchanged.
    /// </summary>
    public string? CustomNamespace { get; set; }

    public ValueLinkGeneratorOptionAttribute()
    {
    }
}
