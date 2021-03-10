// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1615 // Element return value should be documented

namespace CrossLink
{
    public class LinkedListChain<T>
    {
        public LinkedListChain(Func<T, Link> objectToLink)
        {
            this.objectToLink = objectToLink;
        }

        public void AddLast(T obj)
        {
            var link = this.objectToLink(obj);
            if (link.IsLinked)
            {
                this.chain.Remove(link.Node);
            }

            link.Node = this.chain.AddLast(obj);
        }

        public void Remove(T obj)
        {
            var link = this.objectToLink(obj);
            if (link.IsLinked)
            {
                this.chain.Remove(link.Node);
                link.Node = null;
            }
        }

        public int Count => this.chain.Count;

        public T? First => this.chain.First == null ? default(T) : this.chain.First.Value;

        private Func<T, Link> objectToLink;
        private LinkedList<T> chain = new();

        public sealed class Link : ILink<T>
        {
            public Link()
            {
            }

            public bool IsLinked => this.Node != null;

            internal LinkedListNode<T>? Node { get; set; }
        }
    }
}
