// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collection;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1615 // Element return value should be documented

namespace CrossLink
{
    public class ListChain<T> : IList<T>, IReadOnlyList<T>
    {
        public delegate ref Link ObjectToLinkDelegete(T obj);

        public ListChain(ObjectToLinkDelegete objectToLink)
        {
            this.objectToLink = objectToLink;
        }

        public int Count => this.chain.Count;

        private ObjectToLinkDelegete objectToLink;
        private UnorderedList<T> chain = new();

        public struct Link : ILink<T>
        {
            public bool IsLinked { get; internal set; }
        }

        #region ICollection

        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an object to the end of the list.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="obj">The object to be added to the end of the list.</param>
        public void Add(T obj)
        {
            ref Link link = ref this.objectToLink(obj);
            if (link.IsLinked)
            {
                this.chain.Remove(obj);
            }

            this.chain.Add(obj);
            link.IsLinked = true;
        }

        /// <summary>
        /// Removes all elements from the list.
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
        /// <param name="value">The object to remove from the <see cref="UnorderedList{T}"/>. </param>
        /// <returns>true if item is successfully removed.</returns>
        public bool Remove(T value)
        {
            var index = this.IndexOf(value);
            if (index >= 0)
            {
                this.RemoveAt(index);
                return true;
            }

            return false;
        }

        #endregion

        #region IList

        public T this[int index]
        {
            get => this.chain[index];

            set
            {
                if ((uint)index >= (uint)this.chain.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var t = this.chain[index];
                ref Link link = ref this.objectToLink(t);
                link.IsLinked = false;

                this.chain[index] = value;
                link = ref this.objectToLink(value);
                link.IsLinked = true;
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

        #endregion

        #region Enumerator

        public UnorderedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();

        #endregion
    }
}
