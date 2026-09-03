// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace ValueLink;

/// <summary>
/// Stores owned objects in an indexed array with constant-time removal.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <remarks>
/// Insertion and removal can reorder other objects. Adding an existing object moves it to the end. The chain does not provide synchronization.
/// </remarks>
public class ListChain<T> : IList<T>, IReadOnlyList<T>
{
    private const int InitialCapacity = 4;
    private const int MaxCapacity = 0X7FFFFFFF;

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
    /// Initializes a new instance of the <see cref="ListChain{T}"/> class (List).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    public ListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Gets the number of objects currently linked to this chain.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity => this.array.Length;

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private T[] array = new T[InitialCapacity];

    /// <summary>
    /// Represents a link structure that maintains the position of an object within the <see cref="ListChain{T}"/>.
    /// </summary>
    public struct Link : ILink<T>
    {
        /// <summary>
        /// The internal raw index value. A value of 0 indicates the object is not linked, values > 0 represent (actual index + 1).
        /// </summary>
        internal int RawIndex;

        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
        /// <value><c>true</c> if the object is linked to the chain; otherwise, <c>false</c>.</value>
        public bool IsLinked => this.RawIndex > 0;

        /// <summary>
        /// Gets the zero-based index of the object in the list.
        /// </summary>
        /// <value>The zero-based index position of the linked object, or -1 if not linked.</value>
        public int Index
        {
            get => this.RawIndex - 1;
            internal set => this.RawIndex = value + 1;
        }
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds or moves an object to the end of the list in amortized O(1) time.
    /// </summary>
    /// <param name="obj">The object to be added to the end of the list.</param>
    public void Add(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            this.RemoveInternal(link.Index);
        }

        if (this.Count >= this.Capacity)
        {
            this.DoubleCapacity();
        }

        this.array[this.Count++] = obj;
        link.RawIndex = this.Count;
    }

    /// <summary>
    /// Adds or moves an object to the end of the list in amortized O(1) time.
    /// </summary>
    /// <param name="obj">The object to be added to the end of the list.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    public void Add(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.IsLinked)
        {
            this.RemoveInternal(link.Index);
        }

        if (this.Count >= this.Capacity)
        {
            this.DoubleCapacity();
        }

        this.array[this.Count++] = obj;
        link.RawIndex = this.Count;
    }

    /// <summary>
    /// Unlinks all objects from this chain while preserving their owner references.
    /// </summary>
    public void Clear()
    {
        foreach (var x in this)
        {
            ref Link link = ref this.objectToLink(x);
            link.RawIndex = 0;
        }

        Array.Clear(this.array, 0, this.Count);
        this.Count = 0;
    }

    /// <summary>
    /// Determines whether an element is in the list.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>true if value is found in the list.</returns>
    public bool Contains(T value) => this.IndexOf(value) >= 0;

    /// <summary>
    /// Copies all linked objects to the destination array in enumeration order.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => Array.Copy(this.array, 0, array, arrayIndex, this.Count);

    /// <summary>
    /// Copies all linked objects to the destination array in enumeration order.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    /// <summary>
    /// Unlinks the object in O(1) time, moving the last object into the vacated slot.
    /// </summary>
    /// <param name="obj">The object to unlink. </param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            this.RemoveInternal(link.Index);
            link.RawIndex = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unlinks the object in O(1) time, moving the last object into the vacated slot.
    /// </summary>
    /// <param name="obj">The object to unlink. </param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(T obj, ref Link link)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if (link.IsLinked)
        {
            this.RemoveInternal(link.Index);
            link.RawIndex = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces the instance stored in an existing link without changing ownership.
    /// </summary>
    /// <remarks>
    /// For generated copy-on-write updates. The replacement must have the correct owner; this method transfers only this chain's link.
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
            this.array[link.Index] = newInstance;
            ref Link link2 = ref this.objectToLink(newInstance);
            link2.RawIndex = link.RawIndex;
            link.RawIndex = 0;
        }
    }

    #endregion

    #region IList

    /// <summary>
    /// Gets or sets the element at the specified index.<br/>
    /// Setting replaces the element currently at <paramref name="index"/>: the replaced object is
    /// unlinked from the chain, and the new object takes its place.<br/>
    /// If the new object is already linked elsewhere in this chain it is moved, and the list shrinks by one.
    /// </summary>
    /// <param name="index">The zero-based index to look up.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.array[index];
        }

        set
        {
            if (this.objectToGoshujin(value) != this.goshujin)
            {// Check Goshujin
                throw new UnmatchedGoshujinException();
            }

            if ((uint)index >= (uint)this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var previous = this.array[index];
            ref Link newLink = ref this.objectToLink(value);
            if (newLink.IsLinked)
            {
                if (newLink.Index == index)
                {// Already at this position.
                    return;
                }

                // Vacate the slot the new object currently occupies. This may relocate
                // the replaced object, so its position is read from its link afterwards.
                this.RemoveInternal(newLink.Index);
                newLink.RawIndex = 0;
            }

            ref Link previousLink = ref this.objectToLink(previous);
            var target = previousLink.Index;
            previousLink.RawIndex = 0;

            this.array[target] = value;
            newLink.Index = target;
        }
    }

    /// <summary>
    /// Returns the object's link index, or -1 if it is absent or belongs to another owner.
    /// </summary>
    /// <param name="obj">The object to locate in the list.</param>
    /// <returns>The object's zero-based index, or -1 if absent.</returns>
    public int IndexOf(T obj)
    {
        if (obj is null || this.objectToGoshujin(obj) != this.goshujin)
        {
            return -1;
        }

        ref Link link = ref this.objectToLink(obj);
        return link.Index;
    }

    /// <summary>
    /// Inserts or moves an object to the specified index in amortized O(1) time.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="obj">The object to insert.</param>
    /// <remarks>
    /// The displaced object moves to the end. Inserting an existing object at Count moves it to the final index.
    /// </remarks>
    public void Insert(int index, T obj)
    {
        if (this.objectToGoshujin(obj) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        if ((uint)index > (uint)this.Count)
        {
            throw new IndexOutOfRangeException();
        }

        ref Link link = ref this.objectToLink(obj);
        if (link.IsLinked)
        {
            this.RemoveInternal(link.Index);
            if (index > this.Count)
            {// Unlinking the object shrank the list; append instead.
                index = this.Count;
            }
        }

        if (this.Count >= this.Capacity)
        {
            this.DoubleCapacity();
        }

        if (index < this.Count)
        {
            var objToMove = this.array[index];
            ref Link link2 = ref this.objectToLink(objToMove);
            this.array[this.Count] = objToMove;
            link2.Index = this.Count;
        }

        this.array[index] = obj;
        link.Index = index;
        this.Count++;
    }

    /// <summary>
    /// Removes the element at the specified index of the list.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        var obj = this[index];
        ref Link link = ref this.objectToLink(obj);
        link.RawIndex = 0;

        this.RemoveInternal(index);
    }

    #endregion

    #region Enumerator

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this.array, this.Count);

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this.array, this.Count);

    /// <summary>
    /// Returns an enumerator over the objects in this chain.
    /// </summary>
    /// <returns>An enumerator over linked objects in chain order.</returns>
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this.array, this.Count);

    /// <summary>
    /// Enumerates the objects in a list chain by index.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[] array;
        private readonly int count;
        private int index;

        internal Enumerator(T[] array, int count)
        {
            this.array = array;
            this.count = count;
            this.index = -1;
        }

        public T Current => this.array[this.index];

        object IEnumerator.Current => this.Current!;

        public bool MoveNext()
        {
            var next = this.index + 1;
            if (next < this.count)
            {
                this.index = next;
                return true;
            }

            return false;
        }

        public void Reset() => this.index = -1;

        public void Dispose()
        {
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveInternal(int index)
    {
        this.Count--;
        if (index < this.Count)
        {
            var obj = this.array[this.Count];
            this.array[index] = obj;

            ref Link link = ref this.objectToLink(obj);
            link.Index = index;
        }

        this.array[this.Count] = default!;
    }

    private void DoubleCapacity()
    {
        var newCapacity = this.array.Length * 2;
        if ((uint)newCapacity > MaxCapacity)
        {
            newCapacity = MaxCapacity;
        }

        var newArray = new T[newCapacity];
        if (this.Count > 0)
        {
            Array.Copy(this.array, 0, newArray, 0, this.Count);
        }

        this.array = newArray;
    }
}
