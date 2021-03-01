// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CrossLink;

namespace Benchmark.Draft
{
    public sealed class TestGoshujin
    {
        public TestGoshujin()
        {
        }

        public void Add(TestClass x)
        {
            this.IdChain.Add(x.IdLink);
        }

        public void Remove(TestClass x)
        {
            this.IdChain.Remove(x.IdLink);
        }

        public ListChain<TestClass> IdChain = new();
    }

    public class TestClass
    {
        public TestClass(int id)
        {
            this.Id = id;
            // this.IdLink = new ListChain<TestClass>.Link(this);
        }

        public TestGoshujin Goshujin
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

        private TestGoshujin GoshujinInstance = default!;

        public int Id { get; set; }

        public ListChain<TestClass>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new(this));

        private ListChain<TestClass>.Link? IdLinkInstance;

        // public ListChain<TestClass>.Link IdLink;

        public string Name { get; set; } = string.Empty;
    }

    public class TestClass0
    {
        public TestClass0( int id)
        {
            this.Id = id;
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestGoshujin2
    {
        public TestGoshujin2()
        {
        }

        public void Add(TestClass2 x)
        {
            this.IdChain.Add(x);
        }

        public void Remove(TestClass2 x)
        {
            this.IdChain.Remove(x);
        }

        public ListChain2<TestClass2> IdChain = new(static x => x.IdLink);
    }

    public class TestClass2
    {
        public TestClass2(int id)
        {
            this.Id = id;
            // this.IdLink = new ListChain<TestClass>.Link(this);
        }

        public TestGoshujin2 Goshujin
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

        private TestGoshujin2 GoshujinInstance = default!;

        public int Id { get; set; }

        public ListChain2<TestClass2>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new(this));

        private ListChain2<TestClass2>.Link? IdLinkInstance;

        // public ListChain<TestClass>.Link IdLink;

        public string Name { get; set; } = string.Empty;
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark1
    {
        public TestGoshujin goshujin = default!;

        public TestGoshujin2 goshujin2 = default!;

        public List<TestClass0> list = default!;

        public Benchmark1()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.list = new();
            this.list.Add(new TestClass0(10));
            this.list.Add(new TestClass0(1));
            this.list.Add(new TestClass0(2));
            this.list.Add(new TestClass0(5));
            this.list.Add(new TestClass0(3));

            this.goshujin = new();
            new TestClass(10).Goshujin = this.goshujin;
            new TestClass(1).Goshujin = this.goshujin;
            new TestClass(2).Goshujin = this.goshujin;
            new TestClass(5).Goshujin = this.goshujin;
            new TestClass(3).Goshujin = this.goshujin;

            this.goshujin2 = new();
            new TestClass2(10).Goshujin = this.goshujin2;
            new TestClass2(1).Goshujin = this.goshujin2;
            new TestClass2(2).Goshujin = this.goshujin2;
            new TestClass2(5).Goshujin = this.goshujin2;
            new TestClass2(3).Goshujin = this.goshujin2;
        }

        [Benchmark]
        public List<TestClass0> Initialize_List()
        {
            var list = new List<TestClass0>();
            list.Add(new TestClass0(10));
            list.Add(new TestClass0(1));
            list.Add(new TestClass0(2));
            list.Add(new TestClass0(5));
            list.Add(new TestClass0(3));

            return list;
        }

        [Benchmark]
        public TestGoshujin Initialize_CrossLink()
        {
            var g = new TestGoshujin();
            new TestClass(10).Goshujin = g;
            new TestClass(1).Goshujin = g;
            new TestClass(2).Goshujin = g;
            new TestClass(5).Goshujin = g;
            new TestClass(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public TestGoshujin2 Initialize_CrossLink2()
        {
            var g = new TestGoshujin2();
            new TestClass2(10).Goshujin = g;
            new TestClass2(1).Goshujin = g;
            new TestClass2(2).Goshujin = g;
            new TestClass2(5).Goshujin = g;
            new TestClass2(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public int RemoveAdd_List()
        {
            var c = this.list[3];
            this.list.Remove(c);
            this.list.Add(c);
            return this.list.Count;
        }

        [Benchmark]
        public int RemoveAdd_ListFast()
        {
            var c = this.list[3];
            this.list.RemoveAt(3);
            this.list.Add(c);
            return this.list.Count;
        }

        [Benchmark]
        public int RemoveAdd_CrossLink()
        {
            var c = this.goshujin.IdChain[4];
            this.goshujin.IdChain.Remove(c.IdLink);
            this.goshujin.IdChain.Add(c.IdLink);
            return this.goshujin.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_CrossLink2()
        {
            var c = this.goshujin2.IdChain[4];
            this.goshujin2.IdChain.Remove(c);
            this.goshujin2.IdChain.Add(c);
            return this.goshujin2.IdChain.Count;
        }

        /* [Benchmark]
        public int Enumerate_List()
        {
            int n = 0;
            foreach (var x in this.list)
            {
                n += x.Id;
            }

            return n;
        }

        [Benchmark]
        public int Enumerate_CrossLink()
        {
            int n = 0;
            foreach (var x in this.goshujin.IdChain)
            {
                n += x.Id;
            }

            return n;
        }*/
    }
}
