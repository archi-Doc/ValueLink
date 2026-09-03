// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Stores owned objects in a bounded circular window with stable logical positions.
/// </summary>
/// <typeparam name="T">The type of objects in the list.</typeparam>
/// <remarks>
/// Call Resize before adding objects. Membership is always manual; Count excludes holes, while Consumed includes them. The chain does not provide synchronization.
/// </remarks>
public class SlidingListChain<T> : IReadOnlyCollection<T>, ICollection
    where T : class
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
    /// Initializes a new instance of the <see cref="SlidingListChain{T}"/> class (<see cref="SlidingList{T}"/> (Array)).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    public SlidingListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Appends an unlinked object, returning false if the window is full or the object is already linked.
    /// </summary>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <returns>True if added; false if already linked or the window is full.</returns>
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
    /// Appends an unlinked object, returning false if the window is full or the object is already linked.
    /// </summary>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns>True if added; false if already linked or the window is full.</returns>
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
    /// Places an unlinked object at a window position, unlinking any object previously stored there.
    /// </summary>
    /// <param name="position">The position of the object.</param>
    /// <param name="obj">The new object that will be added to the list.</param>
    /// <returns>True if placed; false if already linked or outside the capacity window.</returns>
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
    /// Unlinks the object and advances the window past empty positions at its head.
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
    /// Unlinks the object and advances the window past empty positions at its head.
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
    /// Gets the number of consumed window positions, including holes left by removal.
    /// </summary>
    public int Consumed => this.chain.Consumed;

    /// <summary>
    /// Gets the number of objects currently linked to this chain.
    /// </summary>
    public int Count => ((IReadOnlyCollection<T>)this.chain).Count;

    /// <summary>
    /// Changes the window capacity while preserving logical positions.
    /// </summary>
    /// <param name="capacity">The new size of the <see cref="SlidingListChain{T}"/>.</param>
    /// <returns>True if resized; false if the new capacity is smaller than Consumed.</returns>
    /// <remarks>
    /// Returns false if the capacity is smaller than Consumed. A negative capacity throws ArgumentOutOfRangeException.
    /// </remarks>
    public bool Resize(int capacity) => this.chain.Resize(capacity);

    /// <summary>
    /// Returns the object at a logical position, or null for a hole or an out-of-window position.
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
    /// Gets the logical position at the start of the window, even when empty.
    /// </summary>
    public int StartPosition => this.chain.StartPosition;

    /// <summary>
    /// Gets the exclusive logical end position, where the next object would be appended.
    /// </summary>
    public int EndPosition => this.chain.EndPosition;

    /// <summary>
    /// Finds the first equal object by scanning the window's consumed positions.
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
    private SlidingList<T> chain = new(0);

    /// <summary>
    /// Tracks an object's stable position in a sliding window.
    /// </summary>
    public struct Link : ILink<T>
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
        public bool IsLinked => this.rawPosition != 0;

        /// <summary>
        /// Gets the logical window position, or -1 when the object is unlinked.
        /// </summary>
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
    /// Unlinks all objects from this chain while preserving their owner references.
    /// </summary>
    public void Clear()
    {
        foreach (var x in this.chain)
        {
            this.objectToLink(x) = default;
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
    public SlidingList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

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
