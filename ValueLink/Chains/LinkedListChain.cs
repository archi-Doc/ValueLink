// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Represents a doubly linked list of objects.<br/>
/// Structure: Doubly linked list.
/// </summary>
/// <typeparam name="T">The type of objects in the list.</typeparam>
public class LinkedListChain<T> : IReadOnlyCollection<T>, ICollection
    where T : IObjectToGoshujin
{
    public delegate IGoshujin? ObjectToGoshujinDelegete(T obj);

    public delegate ref Link ObjectToLinkDelegete(T obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkedListChain{T}"/> class (Doubly linked list).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
    /// <param name="objectToLink">ObjectToLinkDelegete.</param>
    public LinkedListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Adds a new object at the start of the list.<br/>
    /// If already present in the list, move it to the start.
    /// </summary>
    /// <param name="obj">The new object to add at the start of the list.</param>
    public void AddFirst(T obj)
    {
        if (obj.Goshujin != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        /*if (link.Node != null)
        {
            this.chain.Remove(link.Node);
        }

        link.Node = this.chain.AddFirst(obj);*/

        if (link.Node != null)
        {
            this.chain.MoveToFirst(link.Node);
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
    public void AddLast(T obj)
    {
        if (obj.Goshujin != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        /*if (link.Node != null)
        {
            this.chain.Remove(link.Node);
        }

        link.Node = this.chain.AddLast(obj);*/

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
        if (obj.Goshujin != this.goshujin)
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
    /// Adds a new object to the end of the list.<br/>
    /// If already present in the list, do not change its position.
    /// </summary>
    /// <param name="obj">The new object that will be added to the end of the list.</param>
    public void TryAddLast(T obj)
    {
        if (obj.Goshujin != this.goshujin)
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
        if (obj.Goshujin != this.goshujin)
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

    public void UnsafeReplaceInstance(T previousInstance, T newInstance)
    {
        if (previousInstance.Goshujin != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(previousInstance);
        if (link.Node != null)
        {
            link.Node.UnsafeChangeValue(newInstance);
        }
    }

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
        if (value != null)
        {
            var c = EqualityComparer<T>.Default;
            return this.chain.FirstOrDefault(x => c.Equals(x, value));
        }
        else
        {
            return this.chain.FirstOrDefault(x => x == null);
        }
    }

    private IGoshujin goshujin;
    private ObjectToLinkDelegete objectToLink;
    private UnorderedLinkedList<T> chain = new();

    public struct Link : ILink<T>
    {
        public bool IsLinked => this.Node != null;

        /// <summary>
        /// Gets the previous object.
        /// </summary>
        public T? Previous => this.Node == null || this.Node.Previous == null ? default(T) : this.Node.Previous.Value;

        /// <summary>
        /// Gets the next object.
        /// </summary>
        public T? Next => this.Node == null || this.Node.Next == null ? default(T) : this.Node.Next.Value;

        internal UnorderedLinkedList<T>.Node? Node { get; set; }
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Removes all objects from the collection.
    /// </summary>
    public void Clear()
    {
        UnorderedLinkedList<T>.Node? node;
        while (true)
        {
            node = this.chain.Last;
            if (node == null)
            {
                break;
            }

            ref Link link = ref this.objectToLink(node.Value);
            this.chain.Remove(node.Value);
            link.Node = null;
        }
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    public UnorderedLinkedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();
}
