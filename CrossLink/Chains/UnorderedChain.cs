// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collection;
using CrossLink;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private

namespace CrossLink
{
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
                link.NodeIndex = -1;
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
            public bool IsLinked => this.NodeIndex >= 0;

            internal int NodeIndex { get; set; } = -1;
        }

        #region ICollection

        public bool IsReadOnly => false;

        /// <summary>
        /// Removes all elements from the list.
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
