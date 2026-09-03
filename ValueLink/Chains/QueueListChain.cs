// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink;

/// <summary>
/// Stores owned objects in a linked first-in, first-out (FIFO) queue.
/// </summary>
/// <typeparam name="T">Specifies the type of objects in the queue.</typeparam>
/// <remarks>
/// Enumeration follows dequeue order. The chain does not provide synchronization.
/// </remarks>
public class QueueListChain<T> : IReadOnlyCollection<T>, ICollection
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
    /// Initializes a new instance of the <see cref="QueueListChain{T}"/> class (Doubly linked list).
    /// </summary>
    /// <param name="goshujin">The instance of Goshujin.</param>
    /// <param name="objectToGoshujin">A delegate that returns an object's owner.</param>
    /// <param name="objectToLink">A delegate that returns a reference to this chain's link.</param>
    public QueueListChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
    {
        this.goshujin = goshujin;
        this.objectToGoshujin = objectToGoshujin;
        this.objectToLink = objectToLink;
    }

    /// <summary>
    /// Gets the number of objects currently linked to this chain.
    /// </summary>
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
    /// Adds an object to the end of the <see cref="QueueListChain{T}"/>.<br/>
    /// If already present in the queue, move it to the end.
    /// </summary>
    /// <param name="obj">The object to add to the <see cref="QueueListChain{T}"/>. </param>
    public void Enqueue(T obj)
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
    /// Adds an object to the end of the <see cref="QueueListChain{T}"/>.<br/>
    /// If already present in the queue, move it to the end.
    /// </summary>
    /// <param name="obj">The object to add to the <see cref="QueueListChain{T}"/>. </param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    public void Enqueue(T obj, ref Link link)
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
    /// Adds an object to the end of the <see cref="QueueListChain{T}"/>.<br/>
    /// If already present in the queue, do not change its position.
    /// </summary>
    /// <param name="obj">The object to add to the <see cref="QueueListChain{T}"/>. </param>
    public void TryEnqueue(T obj)
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
    /// Removes the specified object from the <see cref="QueueListChain{T}"/>.
    /// </summary>
    /// <param name="obj">The object to remove from the <see cref="QueueListChain{T}"/>.</param>
    /// <returns>true if the object is successfully removed.</returns>
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
    /// Removes the specified object from the <see cref="QueueListChain{T}"/>.
    /// </summary>
    /// <param name="obj">The object to remove from the <see cref="QueueListChain{T}"/>.</param>
    /// <param name="link">The reference to a link that holds node information in the chain.</param>
    /// <returns>true if the object is successfully removed.</returns>
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

    private IGoshujin goshujin;
    private ObjectToGoshujinDelegete objectToGoshujin;
    private ObjectToLinkDelegete objectToLink;
    private UnorderedLinkedList<T> chain = new();

    /// <summary>
    /// Tracks an object's queue membership and neighboring objects.
    /// </summary>
    public struct Link : ILink<T>
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently linked to this chain.
        /// </summary>
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
