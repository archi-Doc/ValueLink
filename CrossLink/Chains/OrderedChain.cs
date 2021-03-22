// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
    public class OrderedChain<TKey, TObj>
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

        public void Remove(TObj obj)
        {
            ref Link link = ref this.objectToLink(obj);
            if (link.Node != null)
            {
                this.chain.RemoveNode(link.Node);
                link.Node = null;
            }
        }

        public int Count => this.chain.Count;

        public TObj? First => this.chain.First == null ? default(TObj) : this.chain.First.Value;

        public TObj? Last => this.chain.Last == null ? default(TObj) : this.chain.Last.Value;

        private ObjectToLinkDelegete objectToLink;
        private ObjectToKeyDelegete? objectToKey;
        private OrderedMap<TKey, TObj> chain = new();

        public struct Link : ILink<TObj>
        {
            public bool IsLinked => this.Node != null;

            public TObj? Previous => this.Node == null || this.Node.Previous == null ? default(TObj) : this.Node.Previous.Value;

            public TObj? Next => this.Node == null || this.Node.Next == null ? default(TObj) : this.Node.Next.Value;

            internal OrderedMap<TKey, TObj>.Node? Node { get; set; }
        }
    }

    public class OrderedChainClass
    {
        public sealed class GoshujinClass
        {
            public GoshujinClass()
            {
            }

            public void Add(OrderedChainClass x)
            {
                this.IdChain.Add(x, x.Id2);
            }

            public void Remove(OrderedChainClass x)
            {
                this.IdChain.Remove(x);
            }

            public OrderedChain<int, OrderedChainClass> IdChain { get; } = new(static x => ref x.id, static x => ref x.IdLink);
        }

        public OrderedChainClass(int id)
        {
            this.Id2 = id;
        }

        public GoshujinClass Goshujin
        {
            get => this.GoshujinInstance;
            set
            {
                if (this.GoshujinInstance != null)
                {
                    this.GoshujinInstance.Remove(this);
                }

                this.GoshujinInstance = value;
                this.GoshujinInstance.Add(this);
            }
        }

        private GoshujinClass GoshujinInstance = default!;

        public int Id2 { get; set; }

        private int id;

        public int Id
        {
            get => this.id;
            set
            {
                if (value != this.id)
                {
                    this.id = value;
                    this.GoshujinInstance.IdChain.Add(this, value);
                }
            }
        }

        public OrderedChain<int, OrderedChainClass>.Link IdLink;

        public string Name { get; set; } = string.Empty;
    }
}
