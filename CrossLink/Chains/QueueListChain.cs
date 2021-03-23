// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arc.Collection;

#pragma warning disable SA1124 // Do not use regions

namespace CrossLink
{
    /// <summary>
    /// Represents a first-in, first-out (FIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the stack.</typeparam>
    public class QueueListChain<T> : IReadOnlyCollection<T>, ICollection
    {
        public delegate ref Link ObjectToLinkDelegete(T obj);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueListChain{T}"/> class (Doubly linked list).
        /// </summary>
        /// <param name="objectToLink">ObjectToLinkDelegete.</param>
        public QueueListChain(ObjectToLinkDelegete objectToLink)
        {
            this.objectToLink = objectToLink;
        }

        public int Count => this.chain.Count;

        /// <summary>
        /// Returns the object at the beginning of the <see cref="QueueListChain{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the queue.</returns>
        public T Peek()
        {
            if (this.chain.First == null)
            {
                throw new InvalidOperationException("Queue empty.");
            }

            return this.chain.First.Value;
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="QueueListChain{T}"/>, and if one is present, copies it to the result parameter. The object is not removed from the  <see cref="QueueListChain{T}"/>.
        /// </summary>
        /// <param name="result">If present, the object at the beginning of the<see cref="QueueListChain{T}"/>; otherwise, the default value of T.</param>
        /// <returns>true if there is an object at the beginning of the <see cref="QueueListChain{T}"/>; false if the <see cref="QueueListChain{T}"/> is empty.</returns>
        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            if (this.chain.First == null)
            {
                result = default(T);
                return false;
            }

            result = this.chain.First.Value;
            return true;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="QueueListChain{T}"/>.
        /// </summary>
        /// <returns>The object removed from the beginning of the <see cref="QueueListChain{T}"/>.</returns>
        public T Dequeue()
        {
            if (this.chain.First == null)
            {
                throw new InvalidOperationException("Queue empty.");
            }

            var t = this.chain.First.Value;
            ref Link link = ref this.objectToLink(t);
            if (link.Node != null)
            {
                this.chain.Remove(link.Node);
                link.Node = null;
            }

            return t;
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="QueueListChain{T}"/>, and if one is present, copies it to the result parameter, and removes it from the <see cref="QueueListChain{T}"/>.
        /// </summary>
        /// <param name="result">If present, the object at the beginning of the <see cref="QueueListChain{T}"/>; otherwise, the default value of T.</param>
        /// <returns>true if there is an object at the beginning of the <see cref="QueueListChain{T}"/>; false if the <see cref="QueueListChain{T}"/> is empty.</returns>
        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            if (this.chain.First == null)
            {
                result = default(T);
                return false;
            }

            result = this.chain.First.Value;
            ref Link link = ref this.objectToLink(result);
            if (link.Node != null)
            {
                this.chain.Remove(link.Node);
                link.Node = null;
            }

            return true;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="QueueListChain{T}"/>.
        /// </summary>
        /// <param name="obj">The object to add to the <see cref="QueueListChain{T}"/>. The value can be null for reference types.</param>
        public void Enqueue(T obj)
        {
            ref Link link = ref this.objectToLink(obj);
            if (link.Node != null)
            {
                this.chain.Remove(link.Node);
            }

            link.Node = this.chain.AddLast(obj);
        }

        /// <summary>
        /// Removes the specified object from the <see cref="QueueListChain{T}"/>.
        /// </summary>
        /// <param name="obj">The object to remove from the <see cref="QueueListChain{T}"/>.</param>
        /// <returns>true if the object is successfully removed.</returns>
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

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        public UnorderedLinkedList<T>.Enumerator GetEnumerator() => this.chain.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.chain.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.chain.GetEnumerator();
    }
}
