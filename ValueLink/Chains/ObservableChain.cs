﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace ValueLink;

/// <summary>
/// Represents an observable collection of objects that can be accessed by index.<br/>
/// Structure: ObservableCollection (Array).
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class ObservableChain<T> : IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
{
    public delegate IGoshujin? ObjectToGoshujinDelegete(T obj);

    public delegate ref Link ObjectToLinkDelegete(T obj);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableChain{T}"/> class (List).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
    /// <param name="objectToLink">ObjectToLinkDelegete.</param>
    public ObservableChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    public int Count => this.chain.Count;

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private ObservableCollection<T> chain = new();

    event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
    {
        add => this.chain.CollectionChanged += value;
        remove => this.chain.CollectionChanged -= value;
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => ((INotifyPropertyChanged)this.chain).PropertyChanged += value;
        remove => ((INotifyPropertyChanged)this.chain).PropertyChanged -= value;
    }

    public struct Link : ILink<T>
    {
        public bool IsLinked { get; internal set; }
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    /// <summary>
    /// Adds an object to the end of the collection.
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
            this.chain.Remove(obj);
        }

        this.chain.Add(obj);
        link.IsLinked = true;
    }

    /// <summary>
    /// Adds an object to the end of the collection.
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
            this.chain.Remove(obj);
        }

        this.chain.Add(obj);
        link.IsLinked = true;
    }

    /// <summary>
    /// Removes all objects from the collection.
    /// </summary>
    public void Clear()
    {
        foreach (var x in this)
        {
            ref Link link = ref this.objectToLink(x);
            link.IsLinked = false;
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

        var index = this.IndexOf(obj);
        if (index >= 0)
        {
            this.RemoveAt(index);
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

        var index = this.IndexOf(obj);
        if (index >= 0)
        {
            this.RemoveAt(index);
            return true;
        }

        return false;
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
    public int IndexOf(T obj) => this.chain.IndexOf(obj);

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
            this.chain.Remove(obj);
        }

        this.chain.Insert(index, obj);
        link.IsLinked = true;
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

        this.chain.RemoveAt(index);
        link.IsLinked = false;
    }

    /// <summary>
    /// Moves the object at the specified index to a new location in the collection.
    /// </summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the object to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the object.</param>
    public void Move(int oldIndex, int newIndex) => this.chain.Move(oldIndex, newIndex);

    #endregion

    #region Enumerator

    public IEnumerator<T> GetEnumerator() => this.chain.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();

    void ICollection.CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    #endregion
}
