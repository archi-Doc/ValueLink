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
    /// Represents a collection of objects that is maintained without sorting.
    /// <br/>Structure: Hash table.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TObj">The type of objects in the collection.</typeparam>
    public class UnorderedChain<TKey, TObj> : IReadOnlyCollection<TObj>, ICollection
    {
        public delegate IGoshujin? ObjectToGoshujinDelegete(TObj obj);

        public delegate ref Link ObjectToLinkDelegete(TObj obj);

        public delegate ref TKey ObjectToKeyDelegete(TObj obj);

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedChain{TKey, TObj}"/> class (UnorderedMultiMap).
        /// </summary>
        /// <param name="goshujin">The instance of Goshujin.</param>
        /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
        /// <param name="objectToKey">ObjectToKeyDelegete.</param>
        /// <param name="objectToLink">ObjectToLinkDelegete.</param>
        public UnorderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToKeyDelegete objectToKey, ObjectToLinkDelegete objectToLink)
        {
            this.goshujin = goshujin;
            this.objectToGoshujin = objectToGoshujin;
            this.objectToLink = objectToLink;
            this.objectToKey = objectToKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedChain{TKey, TObj}"/> class (OrderedMultiMap).
        /// </summary>
        /// <param name="goshujin">The instance of Goshujin.</param>
        /// <param name="objectToGoshujin">ObjectToGoshujinDelegete.</param>
        /// <param name="objectToLink">ObjectToLinkDelegete.</param>
        public UnorderedChain(IGoshujin goshujin, ObjectToGoshujinDelegete objectToGoshujin, ObjectToLinkDelegete objectToLink)
        {
            this.goshujin = goshujin;
            this.objectToGoshujin = objectToGoshujin;
            this.objectToLink = objectToLink;
        }

        /// <summary>
        /// Adds a new object to the collection.
        /// <br/>O(1) operation.
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

            if (link.IsLinked)
            {
                this.chain.SetNodeKey(link.NodeIndex, key);
            }
            else
            {
                var result = this.chain.Add(key, obj);
                link.NodeIndex = result.nodeIndex;
            }
        }

        /// <summary>
        /// Removes the specific object from the chain.
        /// <br/>O(1) operation.
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
            if (link.IsLinked)
            {
                this.chain.RemoveNode(link.NodeIndex);
                link.RawIndex = 0;
                return true;
            }
            else
            {
                return false;
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
                this.chain.TryGetValue(key, out var value);
                return value;
            }
        }

        /// <summary>
        /// Gets the first element with the specified key.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The first element with the specified key.</returns>
        public TObj? FindFirst(TKey key)
        {
            this.chain.TryGetValue(key, out var value);
            return value;
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

        private IGoshujin goshujin;
        private ObjectToGoshujinDelegete objectToGoshujin;
        private ObjectToLinkDelegete objectToLink;
        private ObjectToKeyDelegete? objectToKey;
        private UnorderedMultiMap<TKey, TObj> chain = new();

        public struct Link : ILink<TObj>
        {
            public bool IsLinked => this.RawIndex > 0;

            public int NodeIndex
            {
                get => this.RawIndex - 1;
                internal set
                {
                    this.RawIndex = value + 1;
                }
            }

            internal int RawIndex { get; set; }
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
            foreach (var x in this.chain.Values)
            {
                ref Link link = ref this.objectToLink(x);
                link.RawIndex = 0;
            }

            this.chain.Clear();
        }

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.chain).CopyTo(array, index);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        public UnorderedMultiMap<TKey, TObj>.ValueCollection.Enumerator GetEnumerator() => this.chain.Values.GetEnumerator();

        IEnumerator<TObj> IEnumerable<TObj>.GetEnumerator() => this.chain.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.chain.Values.GetEnumerator();
    }
}
