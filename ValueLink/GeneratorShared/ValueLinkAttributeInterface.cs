// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using Arc.Collections;

namespace ValueLink;

/// <summary>
/// Specifies the isolation level of each data.
/// </summary>
public enum IsolationLevel
{
    /// <summary>
    /// There is no implementation for isolation.
    /// </summary>
    None,

    /// <summary>
    /// Lock-based concurrency control.<br/>
    /// using (goshujin.LockObject.EnterScope()).
    /// </summary>
    Serializable,

    /// <summary>
    /// Read committed isolation level.<br/>
    /// Does not guarantee repeatable reads (values may change between reads) and phantom reads may occur.<br/>
    /// The object must inherit from <see cref="IDataLocker{TData}"/>.
    /// </summary>
    ReadCommitted,

    /// <summary>
    /// Same data (primitive types) is guaranteed to be read during the transaction.<br/>
    /// The class must be a record type to specify this level.
    /// </summary>
    RepeatableRead,
}

/// <summary>
/// Specifies the type of chain that represents the relationship between values.
/// </summary>
public enum ChainType
{
    /// <summary>
    /// No link. In case you want to use AutoNotify only.
    /// </summary>
    None,

    /// <summary>
    /// Represents a list of objects (<see cref="ListChain{T}"/>).<br/>
    /// Structure: Array (<see cref="UnorderedList{T}"/>).<br/>
    /// List is not recommended due to the time-consuming nature of adding and removing objects.
    /// </summary>
    List,

    /// <summary>
    /// Represents a doubly linked list of objects (<see cref="LinkedListChain{T}"/>).<br/>
    /// Structure: Doubly linked list (<see cref="UnorderedLinkedList{T}"/>).
    /// </summary>
    LinkedList,

    /// <summary>
    /// Represents a stack list (<see cref="StackListChain{T}"/>).<br/>
    /// Structure: Doubly linked list (<see cref="UnorderedLinkedList{T}"/>).
    /// </summary>
    StackList,

    /// <summary>
    /// Represents a queue list (<see cref="QueueListChain{T}"/>).<br/>
    /// Structure: Doubly linked list (<see cref="UnorderedLinkedList{T}"/>).
    /// </summary>
    QueueList,

    /// <summary>
    /// Represents a collection of sorted objects (<see cref="OrderedChain{TKey, TValue}"/>).<br/>
    /// Structure: Red-Black Tree + Linked List (<see cref="OrderedMultiMap{TKey, TValue}"/>).
    /// </summary>
    Ordered,

    /// <summary>
    /// Represents a collection of objects sorted in reverse order (<see cref="OrderedChain{TKey, TValue}"/>).<br/>
    /// Structure: Red-Black Tree + Linked List (<see cref="OrderedMultiMap{TKey, TValue}"/>).
    /// </summary>
    ReverseOrdered,

    /// <summary>
    /// Represents a collection of objects stored in a hash table (<see cref="UnorderedChain{TKey, TValue}"/>).<br/>
    /// Structure: Hash table (<see cref="UnorderedMultiMap{TKey, TValue}"/>).
    /// </summary>
    Unordered,

    /// <summary>
    /// Represents an observable collection of objects. (<see cref="ObservableChain{T}"/>).<br/>
    /// Structure: Collection(Array) (<see cref="ObservableCollection{T}"/>).
    /// </summary>
    Observable,

    /// <summary>
    /// Represents a sliding list of objects (<see cref="SlidingListChain{T}"/>).<br/>
    /// Structure: Array (<see cref="SlidingList{T}"/>).
    /// </summary>
    SlidingList,
}

/// <summary>
/// Specifies the accessibility of generated Value/Link members.
/// </summary>
public enum ValueLinkAccessibility
{
    /// <summary>
    /// [Default] Value/Link members have public getter, and setter with inherited accessibility.
    /// </summary>
    PublicGetter,

    /// <summary>
    /// Value/Link members have public getter/setter.
    /// </summary>
    Public,

    /// <summary>
    /// Value/Link members have protected getter/setter.
    /// </summary>
    Protected,

    /// <summary>
    /// Value/Link members have private getter/setter.
    /// </summary>
    Private,

    /// <summary>
    /// The accessibility of Value/Link members is exactly the same as it's target member.
    /// </summary>
    Inherit,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkObjectAttribute : Attribute
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

    /// <summary>
    /// Gets or sets a value indicating the isolation level to be implemented in the goshujin class.
    /// </summary>
    public IsolationLevel Isolation { get; set; } = IsolationLevel.None;

    /// <summary>
    /// Gets or sets a value indicating whether or not to make the object rectricted <br/>(sets Goshujin's accessibility to 'internal', ensuring all links have 'AddValue' set to false and their accessibility specified as 'Private') [the default is <see langword="false"/>].
    /// </summary>
    public bool Restricted { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to enable the Integrality feature (a function for synchronizing data).<br/>
    /// The object must have a unique link, and its type must be either a primitive type or a struct.<br/>
    /// <see cref="IsolationLevel"/> must be either <see cref="IsolationLevel.None"/> or <see cref="IsolationLevel.Serializable"/>.
    /// </summary>
    public bool Integrality { get; set; } = false;

    public ValueLinkObjectAttribute()
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
    /// Gets or sets a value indicating whether or not the link is a primary link that is guaranteed to holds all objects in the collection [the default is <see langword="false"/>].
    /// </summary>
    public bool Primary { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not the link is a unique link, and all objects maintain their unique values.<br/>
    /// It is mainly used when the IsolationLevel is RepeatableRead. [the default is <see langword="false"/>].
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Gets or sets a string value which represents the name to be used for the Chain and Link (e.g., NameChain/NameLink).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether or not to link the object automatically when the goshujin is set or changed [the default is <see langword="true"/>].
    /// </summary>
    public bool AutoLink { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to invoke PropertyChanged event when the value has changed [the default is <see langword="false"/>].
    /// </summary>
    public bool AutoNotify { get; set; } = false;

    /// <summary>
    /// Gets or sets a string value which represents the target member(property or field) name of the linkage.<br/>
    /// Only LinkAttribute annotated to constructor is supported.
    /// </summary>
    public string TargetMember { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a string value which represents the name of the chain used in sharing (e.g. PrimaryIdChain).<br/>
    /// Instead of creating a member-specific chain, create a link that references another chain.<br/>
    /// To safely add or remove from the chain, it is necessary to reference the link.
    /// </summary>
    public string UnsafeTargetChain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value which specifies the accessibility of generated Value/Link members [the default is <see cref="ValueLinkAccessibility.PublicGetter"/>].
    /// </summary>
    public ValueLinkAccessibility Accessibility { get; set; } = ValueLinkAccessibility.PublicGetter;

    /// <summary>
    /// Gets or sets a value indicating whether or not to create a value property from the target member [the default is <see langword="false"/>].
    /// </summary>
    public bool AddValue { get; set; } = false;

    public LinkAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ValueLinkGeneratorOptionAttribute : Attribute
{
    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public ValueLinkGeneratorOptionAttribute()
    {
    }
}
