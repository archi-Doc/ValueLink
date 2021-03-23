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
    public class LinkedListChain<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        public delegate ref Link ObjectToLinkDelegete(T obj);

        public LinkedListChain(ObjectToLinkDelegete objectToLink)
        {
            this.objectToLink = objectToLink;
        }

        public void AddFirst(T obj)
        {
            ref Link link = ref this.objectToLink(obj);
            if (link.Node != null)
            {
                this.chain.Remove(link.Node);
            }

            link.Node = this.chain.AddFirst(obj);
        }

        public void AddLast(T obj)
        {
            ref Link link = ref this.objectToLink(obj);
            if (link.Node != null)
            {
                this.chain.Remove(link.Node);
            }

            link.Node = this.chain.AddLast(obj);
        }

        public bool Remove(T obj)
        {
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

        public int Count => this.chain.Count;

        public T? First => this.chain.First == null ? default(T) : this.chain.First.Value;

        public T? Last => this.chain.Last == null ? default(T) : this.chain.Last.Value;

        /// <summary>
        /// Finds the first node that contains the specified value.
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

        private ObjectToLinkDelegete objectToLink;
        private UnorderedLinkedList<T> chain = new();

        public struct Link : ILink<T>
        {
            public bool IsLinked => this.Node != null;

            public T? Previous => this.Node == null || this.Node.Previous == null ? default(T) : this.Node.Previous.Value;

            public T? Next => this.Node == null || this.Node.Next == null ? default(T) : this.Node.Next.Value;

            internal UnorderedLinkedList<T>.Node? Node { get; set; }
        }

        #region ICollection

        public bool IsReadOnly => false;

        void ICollection<T>.Add(T value) => this.AddLast(value);

        /// <summary>
        /// Removes all elements from the list.
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

        /// <summary>
        /// Determines whether an element is in the list.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>true if value is found in the list.</returns>
        public bool Contains(T value) => this.Find(value) != null;

        public void CopyTo(T[] array, int arrayIndex) => this.chain.CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        public UnorderedLinkedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();
    }
}
