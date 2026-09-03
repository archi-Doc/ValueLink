// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Indexes owned objects by key in a hash table, allowing duplicate keys.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TObj">The type of objects in the collection.</typeparam>
/// <remarks>
/// Lookup and updates are expected O(1); collisions and resizing can take O(n). The chain does not provide synchronization.
/// </remarks>
public class UnorderedChain<TKey, TObj> : IReadOnlyCollection<TObj>, ICollection
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
    /// Initializes a new instance of the <see cref="UnorderedChain{TKey, TObj}"/> class.
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    public UnorderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
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

        if (link.IsLinked)
        {
            this.chain.SetNodeKey(link.NodeIndex, key);
        }
        else
        {
            var result = this.chain.Add(key, obj);
            link.NodeIndex = result.NodeIndex;
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

        if (link.IsLinked)
        {
            this.chain.SetNodeKey(link.NodeIndex, key);
        }
        else
        {
            var result = this.chain.Add(key, obj);
            link.NodeIndex = result.NodeIndex;
        }
    }

    /// <summary>
    /// Removes the specified object from the chain in expected O(1) time.
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
        if (link.IsLinked)
        {
            this.chain.RemoveNode(link.NodeIndex);
            link.RawIndex = 0;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the specified object from the chain in expected O(1) time.
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

        if (link.IsLinked)
        {
            this.chain.RemoveNode(link.NodeIndex);
            link.RawIndex = 0;
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
        if (link.IsLinked)
        {
            this.chain.UnsafeChangeValue(link.NodeIndex, newInstance);
        }
    }

    /// <summary>
    /// Returns the backing node array and its used index limit.
    /// </summary>
    /// <remarks>
    /// For internal traversal only; the array may contain unused slots and is invalidated by resizing.
    /// </remarks>
    /// <returns>The backing array and exclusive upper index for used slots.</returns>
    public (UnorderedMap<TKey, TObj>.Node[] Nodes, int Max) UnsafeGetNodes()
        => this.chain.UnsafeGetNodes();

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
            this.chain.TryGetValue(key, out var value);
            return value;
        }
    }

    /// <summary>
    /// Returns the first object with the specified key, or default if none exists.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The first matching object, or default if none exists.</returns>
    public TObj? FindFirst(TKey key)
    {
        this.chain.TryGetValue(key, out var value);
        return value;
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
    /// Determines whether the chain contains the key in expected O(1) time.
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

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private UnorderedMap<TKey, TObj> chain = new(true);

    /// <summary>
    /// Represents a link structure that holds node information for an object in the <see cref="UnorderedChain{TKey, TObj}"/>.
    /// </summary>
    public struct Link : ILink<TObj>
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
        public bool IsLinked => this.RawIndex > 0;

        /// <summary>
        /// Gets the index of the node in the chain.
        /// </summary>
        public int NodeIndex
        {
            get => this.RawIndex - 1;
            internal set
            {
                this.RawIndex = value + 1;
            }
        }

        /// <summary>
        /// Gets or sets the raw index value used internally to track the node association.
        /// </summary>
        internal int RawIndex { get; set; }
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
        foreach (var x in this.chain.Values)
        {
            ref Link link = ref this.objectToLink(x);
            link.RawIndex = 0;
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
    public UnorderedMap<TKey, TObj>.ValueEnumerable.Enumerator GetEnumerator() => this.chain.Values.GetEnumerator();

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
