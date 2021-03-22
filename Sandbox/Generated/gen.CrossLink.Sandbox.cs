// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collection;
using CrossLink;

#nullable enable

namespace Sandbox
{
    public partial class TestClass
    {
        public sealed class GoshujinClass
        {
            public GoshujinClass() {}

            public void Add(TestClass x)
            {
                this.TestChain.AddLast(x);
                this.NameChain.Add(x, x.name);
            }

            public void Remove(TestClass x)
            {
                this.TestChain.Remove(x);
                this.NameChain.Remove(x);
            }

            public LinkedListChain<TestClass> TestChain { get; } = new(static x => ref x.TestLink);
            public OrderedChain<string, TestClass> NameChain { get; } = new(static x => ref x.NameLink);
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

        public int Test
        {
            get => this.id;
            set
            {
                if (!EqualityComparer<int>.Default.Equals(value, this.id))
                {
                    this.id = value;
                }
            }
        }

        public LinkedListChain<TestClass>.Link TestLink;

        public string Name
        {
            get => this.name;
            set
            {
                if (!EqualityComparer<string>.Default.Equals(value, this.name))
                {
                    this.name = value;
                }
            }
        }

        public OrderedChain<string, TestClass>.Link NameLink;

    }

    public partial class TestClass2
    {
        public sealed class Goshu
        {
            public Goshu() {}

            public void Add(TestClass2 x)
            {
                this.Name2Chain.Add(x, x.Name);
                this.IdChain.Add(x, x.id);
            }

            public void Remove(TestClass2 x)
            {
                this.Name2Chain.Remove(x);
                this.IdChain.Remove(x);
            }

            public OrderedChain<string, TestClass2> Name2Chain { get; } = new(static x => ref x.Name2Link);
            public OrderedChain<int, TestClass2> IdChain { get; } = new(static x => ref x.IdLink);
        }

        public Goshu Instance
        {
            get => this.InstanceInstance;
            set
            {
                if (this.InstanceInstance != null)
                {
                    this.InstanceInstance.Remove(this);
                }

                this.InstanceInstance = value;
                this.InstanceInstance.Add(this);
            }
        }

        private Goshu InstanceInstance = default!;

        public string Name2
        {
            get => this.Name;
            set
            {
                if (!EqualityComparer<string>.Default.Equals(value, this.Name))
                {
                    this.Name = value;
                }
            }
        }

        public OrderedChain<string, TestClass2>.Link Name2Link;

        public int Id
        {
            get => this.id;
            set
            {
                if (!EqualityComparer<int>.Default.Equals(value, this.id))
                {
                    this.id = value;
                }
            }
        }

        public OrderedChain<int, TestClass2>.Link IdLink;

    }

    public partial class TestClass3<T>
    {
        public sealed class GoshujinClass
        {
            public GoshujinClass() {}

            public void Add(TestClass3<T> x)
            {
                this.IdChain.Add(x, x.id);
                this.NameChain.Add(x, x.name);
            }

            public void Remove(TestClass3<T> x)
            {
                this.IdChain.Remove(x);
                this.NameChain.Remove(x);
            }

            public OrderedChain<T, TestClass3<T>> IdChain { get; } = new(static x => ref x.IdLink);
            public OrderedChain<string, TestClass3<T>> NameChain { get; } = new(static x => ref x.NameLink);
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

        public T Id
        {
            get => this.id;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, this.id))
                {
                    this.id = value;
                }
            }
        }

        public OrderedChain<T, TestClass3<T>>.Link IdLink;

        public string Name
        {
            get => this.name;
            set
            {
                if (!EqualityComparer<string>.Default.Equals(value, this.name))
                {
                    this.name = value;
                }
            }
        }

        public OrderedChain<string, TestClass3<T>>.Link NameLink;

    }

    public partial class TestClass4
    {

        private partial class NestedClass
        {
            public sealed class GoshujinClass
            {
                public GoshujinClass() {}

                public void Add(NestedClass x)
                {
                    this.IdChain.Add(x, x.id);
                }

                public void Remove(NestedClass x)
                {
                    this.IdChain.Remove(x);
                }

                public OrderedChain<uint, NestedClass> IdChain { get; } = new(static x => ref x.IdLink);
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

            public uint Id
            {
                get => this.id;
                set
                {
                    if (!EqualityComparer<uint>.Default.Equals(value, this.id))
                    {
                        this.id = value;
                    }
                }
            }

            public OrderedChain<uint, NestedClass>.Link IdLink;

        }

    }
}
