// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace ValueLink;

/// <summary>
/// Represents a list of objects that can be accessed by index.<br/>
/// Structure: Array.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class ListChain<T> : IList<T>, IReadOnlyList<T>
{
    public delegate IGoshujin? ObjectToGoshujinDelegete(T obj);

    public delegate ref Link ObjectToLinkDelegete(T obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="ListChain{T}"/> class (List).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
    /// <param name="objectToLink">ObjectToLinkDelegete.</param>
    public ListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    public int Count => this.chain.Count;

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private UnorderedList<T> chain = new();

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
        /// Gets a value indicating whether this link is currently associated with an object in the chain.
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
    /// Adds an object to the end of the list.
    /// <br/>O(1) operation.
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

        this.chain.Add(obj);
        link.RawIndex = this.chain.Count;
    }

    /// <summary>
    /// Adds an object to the end of the list.
    /// <br/>O(1) operation.
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

        this.chain.Add(obj);
        link.RawIndex = this.chain.Count;
    }

    /// <summary>
    /// Removes all objects from the collection.
    /// </summary>
    public void Clear()
    {
        foreach (var x in this)
        {
            ref Link link = ref this.objectToLink(x);
            link.RawIndex = 0;
        }

        this.chain.Clear();
    }

    /// <summary>
    /// Determines whether an element is in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>true if value is found in the list.</returns>
    public bool Contains(T value) => this.IndexOf(value) >= 0;

    /// <summary>
    /// Copies the list or a portion of it to an array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => this.chain.CopyTo(array, arrayIndex);

    /// <summary>
    /// Copies the list or a portion of it to an array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="UnorderedList{T}"/>.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="obj">The object to remove from the <see cref="UnorderedList{T}"/>. </param>
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
    /// Removes the first occurrence of a specific object from the <see cref="UnorderedList{T}"/>.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="obj">The object to remove from the <see cref="UnorderedList{T}"/>. </param>
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

    public void UnsafeReplaceInstance(T previousInstance, T newInstance)
    {
        if (this.objectToGoshujin(previousInstance) != this.goshujin)
        {// Check Goshujin
            throw new UnmatchedGoshujinException();
        }

        ref Link link = ref this.objectToLink(previousInstance);
        if (link.IsLinked)
        {
            this.chain[link.Index] = newInstance;
            ref Link link2 = ref this.objectToLink(newInstance);
            link2.RawIndex = link.RawIndex;
            link.RawIndex = 0;
        }
    }

    #endregion

    #region IList

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get => this.chain[index];

        set
        {
            this.Insert(index, value);
            // throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Returns the zero-based index of the first occurrence of a value in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="obj">The object to locate in the list.</param>
    /// <returns>The zero-based index of the first occurrence of item.</returns>
    public int IndexOf(T obj)
    {
        ref Link link = ref this.objectToLink(obj);
        return link.Index;
    }

    /// <summary>
    /// Inserts an element into the <see cref="UnorderedList{T}"/> at the specified index.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="obj">The object to insert.</param>
    public void Insert(int index, T obj)
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

        this.chain.Insert(index, obj);
        link.Index = index;
        for (var i = index + 1; i < this.chain.Count; i++)
        {
            ref Link link2 = ref this.objectToLink(this.chain[i]);
            link2.RawIndex++;
        }
    }

    /// <summary>
    /// Removes the element at the specified index of the list.
    /// <br/>O(n) operation.
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

    public UnorderedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveInternal(int index)
    {// Decrement the indices of the subsequent objects and remove this object from the list.
        for (var i = index + 1; i < this.chain.Count; i++)
        {
            ref Link link = ref this.objectToLink(this.chain[i]);
            link.RawIndex--;
        }

        this.chain.RemoveAt(index);
    }
}
