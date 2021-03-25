// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collection;
using CrossLink;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private

namespace CrossLink
{
    public class OrderedChain<TKey, TObj> : IEnumerable<TObj>
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

        public void Add(TObj obj, TKey key)
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

        IEnumerator<TObj> IEnumerable<TObj>.GetEnumerator() => this.chain.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.chain.Values.GetEnumerator();

        public int Count => this.chain.Count;

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
    }
}
