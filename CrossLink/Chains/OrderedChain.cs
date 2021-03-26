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
    public class OrderedChain<TKey, TObj> : IReadOnlyCollection<TObj>, ICollection
    {
        public delegate ref Link ObjectToLinkDelegete(TObj obj);

        public delegate ref TKey ObjectToKeyDelegete(TObj obj);

        public OrderedChain(ObjectToKeyDelegete objectToKey, ObjectToLinkDelegete objectToLink)
        {
            this.objectToLink = objectToLink;
            this.objectToKey = objectToKey;
        }

        public OrderedChain(ObjectToLinkDelegete objectToLink)
        {
            this.objectToLink = objectToLink;
        }

        /*public void Add(TObj obj)
        {
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

        public void Add(TKey key, TObj obj)
        {
            ref Link link = ref this.objectToLink(obj);

            if (link.Node != null)
            {
                this.chain.ReplaceNode(link.Node, key);
            }
            else
            {
                var result = this.chain.Add(key, obj);
                link.Node = result.node;
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

        public int Count => this.chain.Count;

        /// <summary>
        /// Gets or sets the element with the specified key.
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

        private ObjectToLinkDelegete objectToLink;
        private ObjectToKeyDelegete? objectToKey;
        private OrderedMultiMap<TKey, TObj> chain = new();

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
