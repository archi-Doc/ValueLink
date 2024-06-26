﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
public class SlidingListChain<T> : IReadOnlyCollection<T>, ICollection
    where T : class
{
    public delegate IGoshujin? ObjectToGoshujinDelegete(T obj);

    public delegate ref Link ObjectToLinkDelegete(T obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingListChain{T}"/> class (<see cref="SlidingList{T}"/> (Array)).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
    /// <param name="objectToLink">ObjectToLinkDelegete.</param>
    public SlidingListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Inserts an object into an available space in the list. If insertion is not possible, returns -1.
    /// </summary>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Add(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            return false;
        }
        else
        {
            link.Position = this.chain.Add(obj);
            return link.IsLinked;
        }
    }

    /// <summary>
    /// Inserts an object into an available space in the list. If insertion is not possible, returns -1.
    /// </summary>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Add(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.IsLinked)
        {
            return false;
        }
        else
        {
            link.Position = this.chain.Add(obj);
            return link.IsLinked;
        }
    }

    /// <summary>
    /// Inserts an object into the specified position in the list. If insertion is not possible, returns <see langword="false"/>.
    /// </summary>
    /// <param name="position">The position of the object.</param>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Set(int position, T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            return false;
        }

        if (this.chain.Get(position) is { } prev)
        {
            ref Link prevLink = ref this.objectToLink(prev);
            prevLink.Position = -1;
        }

        if (this.chain.Set(position, obj))
        {
            link.Position = position;
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
    /// <param name="obj">The object that will be removed from the list.</param>
    /// <returns><see langword="true"/>; The object is successfully removed.</returns>
    public bool Remove(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            var result = this.chain.Remove(link.Position);
            link.Position = -1;
            return result;
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
    /// <param name="obj">The object that will be removed from the list.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns><see langword="true"/>; The object is successfully removed.</returns>
    public bool Remove(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.IsLinked)
        {
            var result = this.chain.Remove(link.Position);
            link.Position = -1;
            return result;
        }
        else
        {
            return false;
        }
    }

    public void UnsafeReplaceInstance(T previousInstance, T newInstance)
    {
        if (this.objectToGoshujin(previousInstance) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(previousInstance);
        if (link.IsLinked)
        {
            this.chain.Set(link.Position, newInstance);
        }
    }

    /// <summary>
    /// Gets the maximum number of elements that <see cref="SlidingListChain{T}"/> can hold.
    /// </summary>
    public int Capacity => this.chain.Capacity;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="SlidingListChain{T}"/>.
    /// </summary>
    public int Consumed => this.chain.Consumed;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="SlidingListChain{T}"/>.
    /// </summary>
    public int Count => this.chain.Consumed;

    // int ICollection.Count => this.chain.Consumed;

    // int IReadOnlyCollection<T>.Count => this.chain.Consumed;

    /// <summary>
    /// Changes the number of elements of the <see cref="SlidingListChain{T}"/> to the specified new size.
    /// </summary>
    /// <param name="capacity">The new size of the <see cref="SlidingListChain{T}"/>.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Resize(int capacity) => this.chain.Resize(capacity);

    /// <summary>
    /// Gets the object at the specified position.
    /// </summary>
    /// <param name="position">The position of the object.</param>
    /// <returns>The object.</returns>
    public T? Get(int position) => this.chain.Get(position);

    /// <summary>
    /// Gets a value indicating whether there is space in the <see cref="SlidingListChain{T}"/> and if a new element can be added.
    /// </summary>
    public bool CanAdd => this.chain.CanAdd;

    /// <summary>
    /// Gets the first element of the <see cref="SlidingListChain{T}"/>, or a default value if the <see cref="SlidingListChain{T}"/> contains no elements.
    /// </summary>
    public T? FirstOrDefault => this.chain.FirstOrDefault;

    /// <summary>
    /// Gets the position of the first element contained in the <see cref="SlidingListChain{T}"/>.
    /// </summary>
    public int StartPosition => this.chain.StartPosition;

    /// <summary>
    /// Gets the position of the last element contained in the <see cref="SlidingListChain{T}"/>.
    /// </summary>
    public int EndPosition => this.chain.EndPosition;

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
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private SlidingList<T> chain = new(0);

    public struct Link : ILink<T>
    {
        public bool IsLinked => this.rawPosition > 0;

        public int Position
        {
            get => this.rawPosition - 1;
            internal set => this.rawPosition = value + 1;
        }

        private int rawPosition;
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Removes all objects from the list.
    /// </summary>
    public void Clear()
    {
        var array = this.chain.ToArray();
        foreach (var x in array)
        {
            if (x is not null)
            {
                this.Remove(x);
            }
        }
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    public SlidingList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();
}
