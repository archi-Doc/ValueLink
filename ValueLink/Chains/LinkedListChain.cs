// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Stores owned objects in a doubly linked list.
/// </summary>
/// <typeparam name="T">The type of objects in the list.</typeparam>
/// <remarks>
/// Links support direct removal and navigation. The chain does not provide synchronization.
/// </remarks>
public class LinkedListChain<T> : IReadOnlyCollection<T>, ICollection
{
    /// <summary>
    /// Returns the owner of an object.
    /// </summary>
    /// <param name="obj">The object whose link or owner is requested.</param>
    /// <returns>The object's owner, or null when unowned.</returns>
    public delegate IGoshujin? ObjectToGoshujinDelegete(T obj);

    /// <summary>
    /// Returns a reference to an object's link for this chain.
    /// </summary>
    /// <param name="obj">The object whose link or owner is requested.</param>
    /// <returns>A reference to the object's link for this chain.</returns>
    public delegate ref Link ObjectToLinkDelegete(T obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkedListChain{T}"/> class (Doubly linked list).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    public LinkedListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Adds a new object at the start of the list.<br/>
    /// If already present in the list, move it to the start.
    /// </summary>
    /// <param name="obj">The new object to add at the start of the list.</param>
    public void AddFirst(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.MoveToFirst(link.Node);
        }
        else
        {
            link.Node = this.chain.AddFirst(obj);
        }
    }

    /// <summary>
    /// Adds a new object to the end of the list.<br/>
    /// If already present in the list, move it to the end.
    /// </summary>
    /// <param name="obj">The new object that will be added to the end of the list.</param>
    public void AddLast(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.MoveToLast(link.Node);
        }
        else
        {
            link.Node = this.chain.AddLast(obj);
        }
    }

    /// <summary>
    /// Adds a new object to the end of the list.<br/>
    /// If already present in the list, move it to the end.
    /// </summary>
    /// <param name="obj">The new object that will be added to the end of the list.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    public void AddLast(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.Node != null)
        {
            this.chain.MoveToLast(link.Node);
        }
        else
        {
            link.Node = this.chain.AddLast(obj);
        }
    }

    /// <summary>
    /// Adds a new object at the start of the list.<br/>
    /// If already present in the list, do not change its position.
    /// </summary>
    /// <param name="obj">The new object to add at the start of the list.</param>
    public void TryAddFirst(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node is null)
        {
            link.Node = this.chain.AddFirst(obj);
        }
    }

    /// <summary>
    /// Adds a new object to the end of the list.<br/>
    /// If already present in the list, do not change its position.
    /// </summary>
    /// <param name="obj">The new object that will be added to the end of the list.</param>
    public void TryAddLast(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node is null)
        {
            link.Node = this.chain.AddLast(obj);
        }
    }

    /// <summary>
    /// Removes the specified object from the list.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="obj">The object that will be removed from the list. </param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
            link.Node = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the specified object from the list.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="obj">The object that will be removed from the list. </param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
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
    public void UnsafeReplaceInstance(T previousInstance, T newInstance)
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
    /// Gets the first object.
    /// </summary>
    public T? First => this.chain.First == null ? default(T) : this.chain.First.Value;

    /// <summary>
    /// Gets the last object.
    /// </summary>
    public T? Last => this.chain.Last == null ? default(T) : this.chain.Last.Value;

    /// <summary>
    /// Finds the first node that contains the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>The first object that contains the specified value, if found; otherwise, null.</returns>
    public T? Find(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var x in this.chain)
        {
            if (comparer.Equals(x, value))
            {
                return x;
            }
        }

        return default;
    }

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private UnorderedLinkedList<T> chain = new();

    /// <summary>
    /// Represents a link in a doubly linked list chain.
    /// </summary>
    public struct Link : ILink<T>
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
        public bool IsLinked => this.Node != null;

        /// <summary>
        /// Gets the previous object in the linked list, or null if this is the first object or not linked.
        /// </summary>
        public T? Previous => this.Node == null || this.Node.Previous == null ? default(T) : this.Node.Previous.Value;

        /// <summary>
        /// Gets the next object in the linked list, or null if this is the last object or not linked.
        /// </summary>
        public T? Next => this.Node == null || this.Node.Next == null ? default(T) : this.Node.Next.Value;

        /// <summary>
        /// Gets or sets the internal node reference in the underlying linked list structure.
        /// </summary>
        internal UnorderedLinkedList<T>.Node? Node { get; set; }
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
        while (this.chain.Last is { } node)
        {
            ref Link link = ref this.objectToLink(node.Value);
            this.chain.Remove(node);
            link.Node = null;
        }
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    public UnorderedLinkedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();
}
