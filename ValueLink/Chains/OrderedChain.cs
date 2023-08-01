// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

#pragma warning disable SA1124 // Do not use regions

namespace ValueLink
{
    /// <summary>
    /// Represents a collection of objects that is maintained in sorted order.
    /// <br/>Structure: Red-Black Tree + Linked List.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TObj">The type of objects in the collection.</typeparam>
    public class OrderedChain<TKey, TObj> : IReadOnlyCollection<TObj>, ICollection
    {
        public delegate IGoshujin? ObjectToGoshujinDelegete(TObj obj);

        public delegate ref Link ObjectToLinkDelegete(TObj obj);

        public delegate ref TKey ObjectToKeyDelegete(TObj obj);

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedChain{TKey, TObj}"/> class (OrderedMultiMap).
        /// </summary>
        /// <param name="goshujin">The instance of Goshujin.</param>
        /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
        /// <param name="objectToKey">ObjectToKeyDelegete.</param>
        /// <param name="objectToLink">ObjectToLinkDelegete.</param>
        /// <param name="reverse">true to reverses the order.</param>
        public OrderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToKeyDelegete objectToKey, ObjectToLinkDelegete objectToLink, bool reverse = false)
        {
            this.chain = new(reverse);
            this.goshujin = goshujin;
            this.objectToGoshujin = objectToGoshujin;
            this.objectToLink = objectToLink;
            this.objectToKey = objectToKey;
            this.Reverse = reverse;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedChain{TKey, TObj}"/> class (OrderedMultiMap).
        /// </summary>
        /// <param name="goshujin">The instance of Goshujin.</param>
        /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
        /// <param name="objectToLink">ObjectToLinkDelegete.</param>
        /// <param name="reverse">true to reverses the order.</param>
        public OrderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink, bool reverse = false)
        {
            this.chain = new(reverse);
            this.goshujin = goshujin;
            this.objectToGoshujin = objectToGoshujin;
            this.objectToLink = objectToLink;
        }

        /*public void Add(TObj obj)
        {
            if (this.objectToGoshujin(obj) != this.goshujin)
            {// Check Goshujin
                throw new UnmatchedGoshujinException();
            }

            if (this.objectToKey == null)
            {
                throw new InvalidOperationException();
            }

            ref Link link = ref this.objectToLink(obj);
            ref TKey key = ref this.objectToKey(obj);

            if (link.Node != null)
            {
                this.chain.ReplaceNode(link.Node, key);
            }
            else
            {
                var result = this.chain.Add(key, obj);
                link.Node = result.node;
            }
        }*/

        /// <summary>
        /// Adds a new object to the collection.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the object to add.</param>
        /// <param name="obj">The object to add.</param>
        public void Add(TKey key, TObj obj)
        {
            if (this.objectToGoshujin(obj) != this.goshujin)
            {// Check Goshujin
                throw new UnmatchedGoshujinException();
            }

            ref Link link = ref this.objectToLink(obj);

            if (link.Node != null)
            {
                this.chain.SetNodeKey(link.Node, key);
            }
            else
            {
                var result = this.chain.Add(key, obj);
                link.Node = result.Node;
            }
        }

        /// <summary>
        /// Removes the specific object from the chain.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="obj">The object to remove from the chain. </param>
        /// <returns>true if item is successfully removed.</returns>
        public bool Remove(TObj obj)
        {
            if (this.objectToGoshujin(obj) != this.goshujin)
            {// Check Goshujin
                throw new UnmatchedGoshujinException();
            }

            ref Link link = ref this.objectToLink(obj);
            if (link.Node != null)
            {
                this.chain.RemoveNode(link.Node);
                link.Node = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UnsafeReplaceInstance(TObj previousInstance, TObj newInstance)
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

        public int Count => this.chain.Count;

        /// <summary>
        /// Gets the element with the specified key.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The element with the specified key.</returns>
        public TObj? this[TKey key]
        {
            get
            {
                var node = this.chain.FindFirstNode(key);
                return node == null ? default : node.Value;
            }
        }

        /// <summary>
        /// Gets the first element with the specified key.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The first element with the specified key.</returns>
        public TObj? FindFirst(TKey key)
        {
            var node = this.chain.FindFirstNode(key);
            return node == null ? default : node.Value;
        }

        /// <summary>
        /// Enumerates elements with the specified key.
        /// </summary>
        /// <param name="key">The key to search in a collection.</param>
        /// <returns>The elements with the specified key.</returns>
        public IEnumerable<TObj> Enumerate(TKey? key) => this.chain.EnumerateValue(key);

        public IEnumerable<TKey> Keys => this.chain.Keys;

        public IEnumerable<TObj> Objects => this.chain.Values;

        public IEnumerable<KeyValuePair<TKey, TObj>> KeyObjects => this.chain;

        /// <summary>
        /// Determines whether the chain contains an element with the specified key.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key to locate in the chain.</param>
        /// <returns>true if the chain contains an element with the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key) => this.chain.ContainsKey(key);

        /// <summary>
        /// Gets the object associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="obj">When this method returns, the value associated with the specified key, if the key is found.</param>
        /// <returns>true if the chain contains an element with the key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TObj obj) => this.chain.TryGetValue(key, out obj);

        /// <summary>
        /// Gets the first object.
        /// </summary>
        public TObj? First => this.chain.First == null ? default(TObj) : this.chain.First.Value;

        /// <summary>
        /// Gets the last object.
        /// </summary>
        public TObj? Last => this.chain.Last == null ? default(TObj) : this.chain.Last.Value;

        /// <summary>
        /// Returns the first object with a key that is equal to or greater than the specified key.<br/>If it returns null, it means that the keys of all objects are less than or equal to the specified value.
        /// </summary>
        /// <param name="key">The specified key.</param>
        /// <returns>The first object with a key that is equal to or greater than the specified key.</returns>
        public TObj? GetLowerBound(TKey key)
        {
            var node = this.chain.GetLowerBound(key);
            return node == null ? default : node.Value;
        }

        /// <summary>
        /// Returns the last object with a key that is equal to or lower than the specified key.<br/>If it returns null, it means that the keys of all objects are greater than or equal to the specified value.
        /// </summary>
        /// <param name="key">The specified key.</param>
        /// <returns>The last object with a key that is equal to or lower than the specified key.</returns>
        public TObj? GetUpperBound(TKey key)
        {
            var node = this.chain.GetUpperBound(key);
            return node == null ? default : node.Value;
        }

        /// <summary>
        /// Specify the lower and upper bounds of the key, and return the first and last objects within that range.
        /// </summary>
        /// <param name="lower">The lower bound key.</param>
        /// <param name="upper">The upper bound key.</param>
        /// <returns>The first and last objects within that range.</returns>
        public (TObj? Lower, TObj? Upper) GetRange(TKey lower, TKey upper)
        {
            var range = this.chain.GetRange(lower, upper);
            return (range.Lower is null ? default : range.Lower.Value, range.Upper is null ? default : range.Upper.Value);
        }

        /// <summary>
        /// Gets a value indicating whether or not the collection is in reverse order.
        /// </summary>
        public bool Reverse { get; }

        private IGoshujin goshujin;
        private ObjectToGoshujinDelegete objectToGoshujin;
        private ObjectToLinkDelegete objectToLink;
        private ObjectToKeyDelegete? objectToKey;
        private OrderedMultiMap<TKey, TObj> chain;

        public struct Link : ILink<TObj>
        {
            public bool IsLinked => this.Node != null;

            /// <summary>
            /// Gets the previous object.
            /// </summary>
            public TObj? Previous => this.Node == null || this.Node.Previous == null ? default(TObj) : this.Node.Previous.Value;

            /// <summary>
            /// Gets the next object.
            /// </summary>
            public TObj? Next => this.Node == null || this.Node.Next == null ? default(TObj) : this.Node.Next.Value;

            internal OrderedMultiMap<TKey, TObj>.Node? Node { get; set; }
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
            while (true)
            {
                var node = this.chain.Last;
                if (node == null)
                {
                    break;
                }

                ref Link link = ref this.objectToLink(node.Value);
                this.chain.RemoveNode(link.Node!);
                link.Node = null;
            }
        }

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        public OrderedMultiMap<TKey, TObj>.ValueCollection.Enumerator GetEnumerator() => this.chain.Values.GetEnumerator();

        IEnumerator<TObj> IEnumerable<TObj>.GetEnumerator() => this.chain.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.chain.Values.GetEnumerator();
    }
}
