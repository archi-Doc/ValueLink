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
            this.IdChain.Add(x, x.IdLink);
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

        private TestGoshujin GoshujinInstance;

        public int Id { get; set; }

        public ListChain<TestClass>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new());

        private ListChain<TestClass>.Link? IdLinkInstance;

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

        public TestClass target = default!;


        public List<TestClass2> list = default!;

        public TestClass2 target2 = default!;

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
            target2 = new TestClass2(5);
            this.list.Add(target2);
            this.list.Add(new TestClass2(3));

            this.goshujin = new();
            new TestClass(10).Goshujin = this.goshujin;
            new TestClass(1).Goshujin = this.goshujin;
            new TestClass(2).Goshujin = this.goshujin;
            this.target = new TestClass(5);
            this.target.Goshujin = this.goshujin;
            new TestClass(3).Goshujin = this.goshujin;
        }

        [Benchmark]
        public bool Remove_List()
        {
            return this.list.Remove(this.target2);
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
        public bool Remove_CrossLink()
        {
            return this.goshujin.IdChain.Remove(this.target, this.target.IdLink);
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
