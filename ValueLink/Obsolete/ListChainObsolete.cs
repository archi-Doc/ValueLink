// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1615 // Element return value should be documented

namespace ValueLink.Obsolete
{
    public sealed class ListChainObsolete<T> : IEnumerable<T>
    {
        private const int MaxArrayLength = 0X7FEFFFFF;
        private const int DefaultCapacity = 4;

        private T[] items;
        private int size;
        private int version;

        public ListChainObsolete()
        {
            this.items = Array.Empty<T>();
        }

        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return this.items[index];
            }

            set
            {
                if ((uint)index >= (uint)this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                this.items[index] = value;
                this.version++;
            }
        }

        public int Count => this.size;

        public bool Add(Link link)
        {
            if (link.IsLinked)
            {
                this.Remove(link);
            }

            if (this.size == this.items.Length)
            {
                this.EnsureCapacity(this.size + 1);
            }

            link.index = this.size;
            this.items[this.size++] = link.obj;
            this.version++;

            return true;
        }

        public bool Remove(Link link)
        {
            if (!link.IsLinked)
            {
                return false;
            }

            if ((uint)link.index >= (uint)this.size)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (link.obj?.Equals(this.items[link.index]) != true)
            {
                throw new InvalidOperationException();
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

        private void EnsureCapacity(int min)
        {
            if (this.items.Length < min)
            {
                int newCapacity = this.items.Length == 0 ? DefaultCapacity : this.items.Length * 2;

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
                        this.items = Array.Empty<T>();
                    }
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public sealed class Link : ILink<T>
        {
            internal T obj;
            internal int index = -1;

            public Link(T obj)
            {
                this.obj = obj;
            }

            public T Object => this.obj;

            public bool IsLinked => this.index >= 0;

            public int Index => this.index;
        }

        /*public struct Link : ILink
        {
            internal T obj;
            internal int rawIndex;

            public Link(T obj, int index)
            {
                this.obj = obj;
                this.rawIndex = index + 1;
            }

            public Link(T obj)
            {
                this.obj = obj;
                this.rawIndex = 0;
            }

            public int Index
            {
                get => this.rawIndex - 1;
                set
                {
                    this.rawIndex = value + 1;
                }
            }

            public bool IsLinked => this.rawIndex > 0;
        }*/

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private ListChainObsolete<T> list;
            private int index;
            private int version;
            private T? current;

            internal Enumerator(ListChainObsolete<T> list)
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

                if (this.version != this.list.version)
                {
                    throw new Exception();
                }

                this.index = this.list.size + 1;
                this.current = default(T);
                return false;
            }

            public T Current => this.current!;

            object IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || this.index == this.list.size + 1)
                    {
                        throw new Exception();
                    }

                    return this.Current!;
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
