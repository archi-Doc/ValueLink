// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Indexes owned objects by key in a red-black tree, allowing duplicate keys.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TObj">The type of objects in the collection.</typeparam>
/// <remarks>
/// The reverse option changes traversal and comparison order. The chain does not provide synchronization.
/// </remarks>
public class OrderedChain<TKey, TObj> : IReadOnlyCollection<TObj>, ICollection
{
    /// <summary>
    /// Returns the owner of an object.
    /// </summary>
    /// <param name="obj">The object whose link or owner is requested.</param>
    /// <returns>The object's owner, or null when unowned.</returns>
    public delegate IGoshujin? ObjectToGoshujinDelegete(TObj obj);

    /// <summary>
    /// Returns a reference to an object's link for this chain.
    /// </summary>
    /// <param name="obj">The object whose link or owner is requested.</param>
    /// <returns>A reference to the object's link for this chain.</returns>
    public delegate ref Link ObjectToLinkDelegete(TObj obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedChain{TKey, TObj}"/> class (OrderedMultiMap).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    /// <param name="reverse">true to reverses the order.</param>
    public OrderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink, bool reverse = false)
    {
        this.chain = new(reverse);
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
        this.Reverse = reverse;
    }

    /// <summary>
    /// Adds an object or updates the key of its existing link.
    /// </summary>
    /// <param name="key">The key of the object to add.</param>
    /// <param name="obj">The object to add.</param>
    public void Add(TKey key, TObj obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);

        if (link.Node != null)
        {
            this.chain.SetNodeKey(link.Node, key);
        }
        else
        {
            var result = this.chain.Add(key, obj);
            link.Node = result.Node;
        }
    }

    /// <summary>
    /// Adds an object or updates the key of its existing link.
    /// </summary>
    /// <param name="key">The key of the object to add.</param>
    /// <param name="obj">The object to add.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    public void Add(TKey key, TObj obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.Node != null)
        {
            this.chain.SetNodeKey(link.Node, key);
        }
        else
        {
            var result = this.chain.Add(key, obj);
            link.Node = result.Node;
        }
    }

    /// <summary>
    /// Removes the specified object from the chain.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="obj">The object to remove from the chain. </param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(TObj obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.RemoveNode(link.Node);
            link.Node = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the specified object from the chain.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="obj">The object to remove from the chain. </param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(TObj obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.Node != null)
        {
            this.chain.RemoveNode(link.Node);
            link.Node = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Replaces the instance stored in an existing link without changing ownership.
    /// </summary>
    /// <remarks>
    /// For generated copy-on-write updates. The replacement must already carry the correct owner and copied link state.
    /// </remarks>
    /// <param name="previousInstance">The instance currently stored in the chain.</param>
    /// <param name="newInstance">The replacement instance with the required owner and link state.</param>
    public void UnsafeReplaceInstance(TObj previousInstance, TObj newInstance)
    {
        if (this.objectToGoshujin(previousInstance) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(previousInstance);
        if (link.Node != null)
        {
            link.Node.UnsafeChangeValue(newInstance);
        }
    }

    /// <summary>
    /// Gets the number of objects currently linked to this chain.
    /// </summary>
    public int Count => this.chain.Count;

    /// <summary>
    /// Gets the first object with the specified key, or default if none exists.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The first matching object, or default if none exists.</returns>
    public TObj? this[TKey key]
    {
        get
        {
            var node = this.chain.FindFirstNode(key);
            return node == null ? default : node.Value;
        }
    }

    /// <summary>
    /// Returns the first object with the specified key, or default if none exists.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The first matching object, or default if none exists.</returns>
    public TObj? FindFirst(TKey key)
    {
        var node = this.chain.FindFirstNode(key);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// Enumerates elements with the specified key.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <returns>The elements with the specified key.</returns>
    public IEnumerable<TObj> Enumerate(TKey? key) => this.chain.EnumerateValue(key);

    /// <summary>
    /// Gets the keys in chain enumeration order, including duplicates.
    /// </summary>
    public IEnumerable<TKey> Keys => this.chain.Keys;

    /// <summary>
    /// Gets the objects in chain enumeration order.
    /// </summary>
    public IEnumerable<TObj> Objects => this.chain.Values;

    /// <summary>
    /// Gets the key-object pairs in chain enumeration order.
    /// </summary>
    public IEnumerable<KeyValuePair<TKey, TObj>> KeyObjects => this.chain;

    /// <summary>
    /// Determines whether the chain contains an element with the specified key.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="key">The key to locate in the chain.</param>
    /// <returns>true if the chain contains an element with the key; otherwise, false.</returns>
    public bool ContainsKey(TKey key) => this.chain.ContainsKey(key);

    /// <summary>
    /// Gets the object associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="obj">When this method returns, the value associated with the specified key, if the key is found.</param>
    /// <returns>true if the chain contains an element with the key; otherwise, false.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TObj obj) => this.chain.TryGetValue(key, out obj);

    /// <summary>
    /// Gets the first object.
    /// </summary>
    public TObj? First => this.chain.First == null ? default(TObj) : this.chain.First.Value;

    /// <summary>
    /// Gets the last object.
    /// </summary>
    public TObj? Last => this.chain.Last == null ? default(TObj) : this.chain.Last.Value;

    /// <summary>
    /// Returns the first object whose key is at least the requested key in the chain's comparison order.
    /// </summary>
    /// <param name="key">The specified key.</param>
    /// <returns>The first matching bound in comparison order, or default if none exists.</returns>
    public TObj? GetLowerBound(TKey key)
    {
        var node = this.chain.GetLowerBound(key);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// Returns the last object whose key is at most the requested key in the chain's comparison order.
    /// </summary>
    /// <param name="key">The specified key.</param>
    /// <returns>The last matching bound in comparison order, or default if none exists.</returns>
    public TObj? GetUpperBound(TKey key)
    {
        var node = this.chain.GetUpperBound(key);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// Returns the first and last objects in an inclusive range in the chain's comparison order.
    /// </summary>
    /// <param name="lower">The lower bound key.</param>
    /// <param name="upper">The upper bound key.</param>
    /// <returns>The inclusive endpoints, or a pair of default values if the range is empty.</returns>
    public (TObj? Lower, TObj? Upper) GetRange(TKey lower, TKey upper)
    {
        var range = this.chain.GetRange(lower, upper);
        return (range.Lower is null ? default : range.Lower.Value, range.Upper is null ? default : range.Upper.Value);
    }

    /// <summary>
    /// Gets a value indicating whether or not the collection is in reverse order.
    /// </summary>
    public bool Reverse { get; }

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private OrderedMultiMap<TKey, TObj> chain;

    /// <summary>
    /// Represents a link to a node within an <see cref="OrderedChain{TKey, TObj}"/>.
    /// </summary>
    /// <remarks>
    /// The link is used to track the position of an object inside the internal ordered structure
    /// and to navigate to the previous or next object in the chain.
    /// </remarks>
    public struct Link : ILink<TObj>
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
        public bool IsLinked => this.Node != null;

        /// <summary>
        /// Gets the previous object in the chain, or <c>default</c> if this link is not associated
        /// with a node or there is no previous node.
        /// </summary>
        public TObj? Previous => this.Node == null || this.Node.Previous == null ? default(TObj) : this.Node.Previous.Value;

        /// <summary>
        /// Gets the next object in the chain, or <c>default</c> if this link is not associated
        /// with a node or there is no next node.
        /// </summary>
        public TObj? Next => this.Node == null || this.Node.Next == null ? default(TObj) : this.Node.Next.Value;

        /// <summary>
        /// Gets or sets the underlying node in the <see cref="OrderedMultiMap{TKey, TObj}"/> that this link refers to.
        /// </summary>
        internal OrderedMultiMap<TKey, TObj>.Node? Node { get; set; }
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Unlinks all objects from this chain while preserving their owner references.
    /// </summary>
    public void Clear()
    {
        var node = this.chain.First;
        while (node is not null)
        {
            this.objectToLink(node.Value) = default;
            node = node.Next;
        }

        this.chain.Clear();
    }

    void ICollection.CopyTo(Array array, int index) => Internal.ChainHelper.CopyTo(this, array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    public OrderedMultiMap<TKey, TObj>.ValueEnumerable.Enumerator GetEnumerator() => this.chain.Values.GetEnumerator();

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    IEnumerator<TObj> IEnumerable<TObj>.GetEnumerator() => this.chain.Values.GetEnumerator();

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    IEnumerator IEnumerable.GetEnumerator() => this.chain.Values.GetEnumerator();
}
