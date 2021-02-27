// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace CrossLink
{
    public sealed class ListChain<T> : IEnumerable<T>
    {
        private const int MaxArrayLength = 0X7FEFFFFF;
        private const int DefaultCapacity = 4;
        private static readonly T[] EmptyArray = new T[0];
        private T[] items;
        private int size;
        private int version;

        public ListChain()
        {
            this.items = EmptyArray;
        }

        public bool Add(T t, Link link)
        {
            if (link.index >= 0)
            {
                return false;
            }

            if (this.size == this.items.Length)
            {
                this.EnsureCapacity(this.size + 1);
            }

            link.index = this.size;
            this.items[this.size++] = t;
            this.version++;

            return true;
        }

        public bool Remove(T item, Link link)
        {
            if (link.index >= 0)
            {
                if ((uint)link.index >= (uint)this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                this.size--;
                if (link.index < this.size)
                {
                    Array.Copy(this.items, link.index + 1, this.items, link.index, this.size - link.index);
                }

                link.index = -1;
                this.items[this.size] = default(T)!;
                this.version++;

                return true;
            }

            return false;
        }

        private void EnsureCapacity(int min)
        {
            if (this.items.Length < min)
            {
                int newCapacity = this.items.Length == 0 ? DefaultCapacity : this.items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > MaxArrayLength)
                {
                    newCapacity = MaxArrayLength;
                }

                if (newCapacity < min)
                {
                    newCapacity = min;
                }

                this.Capacity = newCapacity;
            }
        }

        public int Capacity
        {
            get => this.items.Length;
            set
            {
                if (value < this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != this.items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (this.size > 0)
                        {
                            Array.Copy(this.items, 0, newItems, 0, this.size);
                        }

                        this.items = newItems;
                    }
                    else
                    {
                        this.items = EmptyArray;
                    }
                }
            }
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public sealed class Link
        {
            internal int index = -1;

            public Link()
            {
            }

            public int Index => this.index;
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private ListChain<T> list;
            private int index;
            private int version;
            private T current;

            internal Enumerator(ListChain<T> list)
            {
                this.list = list;
                this.index = 0;
                this.version = list.version;
                this.current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var localList = this.list;

                if (this.version == localList.version && ((uint)this.index < (uint)localList.size))
                {
                    this.current = localList.items[this.index];
                    this.index++;
                    return true;
                }

                return this.MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (this.version != this.list.version)
                {
                    throw new Exception();
                }

                this.index = this.list.size + 1;
                this.current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || this.index == this.list.size + 1)
                    {
                        throw new Exception();
                    }

                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (this.version != this.list.version)
                {
                    throw new Exception();
                }

                this.index = 0;
                this.current = default(T);
            }

        }
    }
}
