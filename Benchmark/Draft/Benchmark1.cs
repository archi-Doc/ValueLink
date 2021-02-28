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
    public class TestGoshujin
    {
        public TestGoshujin()
        {

        }

        public void Add(TestClass x)
        {
            this.IdChain.Add(ref x.IdLink);
        }

        public void Remove(TestClass x)
        {
        }

        public ListChain<TestClass> IdChain = new();
    }

    public class TestClass
    {
        public TestClass(int id)
        {
            this.Id = id;
            this.IdLink = new ListChain<TestClass>.Link(this);
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

        /* public ListChain<TestClass>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new());

        private ListChain<TestClass>.Link? IdLinkInstance;*/

        public ListChain<TestClass>.Link IdLink;

        public string Name { get; set; } = string.Empty;
    }

    public class TestClass2
    {
        public TestClass2( int id)
        {
            this.Id = id;
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark1
    {
        public TestGoshujin goshujin = default!;

        public List<TestClass2> list = default!;

        public Benchmark1()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.list = new();
            this.list.Add(new TestClass2(10));
            this.list.Add(new TestClass2(1));
            this.list.Add(new TestClass2(2));
            this.list.Add(new TestClass2(5));
            this.list.Add(new TestClass2(3));

            this.goshujin = new();
            new TestClass(10).Goshujin = this.goshujin;
            new TestClass(1).Goshujin = this.goshujin;
            new TestClass(2).Goshujin = this.goshujin;
            new TestClass(5).Goshujin = this.goshujin;
            new TestClass(3).Goshujin = this.goshujin;
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
        public int RemoveAdd_CrossLink()
        {
            var c = this.goshujin.IdChain[3];
            this.goshujin.IdChain.Remove(ref c.IdLink);
            this.goshujin.IdChain.Add(ref c.IdLink);
            return this.goshujin.IdChain.Count;
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
        }
    }
}
