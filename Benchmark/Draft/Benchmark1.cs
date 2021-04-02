// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CrossLink;
using CrossLink.Obsolete;

namespace Benchmark.Draft
{
    public sealed class TestGoshujinObsolete
    {
        public TestGoshujinObsolete()
        {
        }

        public void Add(TestClassObsolete x)
        {
            this.IdChain.Add(x.IdLink);
        }

        public void Remove(TestClassObsolete x)
        {
            this.IdChain.Remove(x.IdLink);
        }

        public ListChainObsolete<TestClassObsolete> IdChain = new();
    }

    public class TestClassObsolete
    {
        public TestClassObsolete(int id)
        {
            this.Id = id;
            // this.IdLink = new ListChain<TestClass>.Link(this);
        }

        public TestGoshujinObsolete Goshujin
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

        private TestGoshujinObsolete GoshujinInstance = default!;

        public int Id { get; set; }

        public ListChainObsolete<TestClassObsolete>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new(this));

        private ListChainObsolete<TestClassObsolete>.Link? IdLinkInstance;

        // public ListChain<TestClass>.Link IdLink;

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestGoshujin : IGoshujin
    {
        public TestGoshujin()
        {
            this.IdChain = new(this, static x => x.Goshujin, static x => ref x.IdLink);
        }

        public void Add(TestClass x)
        {
            this.IdChain.Add(x);
        }

        public void Remove(TestClass x)
        {
            this.IdChain.Remove(x);
        }

        public ListChain<TestClass> IdChain { get; }
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

        public ListChain<TestClass>.Link IdLink;

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
        public TestGoshujinObsolete goshujin = default!;

        public TestGoshujin goshujin1 = default!;

        public TestGoshujin2 goshujin2 = default!;

        public TestGoshujin3 goshujin3 = default!;

        public TestGoshujin4 goshujin4 = default!;

        public TestClass4 testClass43 = default!;

        public TestGoshujin5 goshujin5 = default!;

        public TestClass5 testClass53 = default!;

        public TestGoshujin6 goshujin6 = default!;

        public TestClass6 testClass63 = default!;

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
            new TestClassObsolete(10).Goshujin = this.goshujin;
            new TestClassObsolete(1).Goshujin = this.goshujin;
            new TestClassObsolete(2).Goshujin = this.goshujin;
            new TestClassObsolete(5).Goshujin = this.goshujin;
            new TestClassObsolete(3).Goshujin = this.goshujin;

            this.goshujin1 = new();
            new TestClass(10).Goshujin = this.goshujin1;
            new TestClass(1).Goshujin = this.goshujin1;
            new TestClass(2).Goshujin = this.goshujin1;
            new TestClass(5).Goshujin = this.goshujin1;
            new TestClass(3).Goshujin = this.goshujin1;

            this.goshujin2 = new();
            new TestClass2(10).Goshujin = this.goshujin2;
            new TestClass2(1).Goshujin = this.goshujin2;
            new TestClass2(2).Goshujin = this.goshujin2;
            new TestClass2(5).Goshujin = this.goshujin2;
            new TestClass2(3).Goshujin = this.goshujin2;

            this.goshujin3 = new();
            new TestClass3(10).Goshujin = this.goshujin3;
            new TestClass3(1).Goshujin = this.goshujin3;
            new TestClass3(2).Goshujin = this.goshujin3;
            new TestClass3(5).Goshujin = this.goshujin3;
            new TestClass3(3).Goshujin = this.goshujin3;

            this.goshujin4 = new();
            new TestClass4(10).Goshujin = this.goshujin4;
            new TestClass4(1).Goshujin = this.goshujin4;
            new TestClass4(2).Goshujin = this.goshujin4;
            this.testClass43 = new TestClass4(5);
            this.testClass43.Goshujin = this.goshujin4;
            new TestClass4(3).Goshujin = this.goshujin4;

            this.goshujin5 = new();
            new TestClass5(10).Goshujin = this.goshujin5;
            new TestClass5(1).Goshujin = this.goshujin5;
            new TestClass5(2).Goshujin = this.goshujin5;
            this.testClass53 = new TestClass5(5);
            this.testClass53.Goshujin = this.goshujin5;
            new TestClass5(3).Goshujin = this.goshujin5;

            this.goshujin6 = new();
            new TestClass6(10).Goshujin = this.goshujin6;
            new TestClass6(1).Goshujin = this.goshujin6;
            new TestClass6(2).Goshujin = this.goshujin6;
            this.testClass63 = new TestClass6(5);
            this.testClass63.Goshujin = this.goshujin6;
            new TestClass6(3).Goshujin = this.goshujin6;
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
        public TestGoshujinObsolete Initialize_CrossLinkObsolete()
        {
            var g = new TestGoshujinObsolete();
            new TestClassObsolete(10).Goshujin = g;
            new TestClassObsolete(1).Goshujin = g;
            new TestClassObsolete(2).Goshujin = g;
            new TestClassObsolete(5).Goshujin = g;
            new TestClassObsolete(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public TestGoshujin Initialize_CrossLink1()
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
        public TestGoshujin3 Initialize_CrossLink3()
        {
            var g = new TestGoshujin3();
            new TestClass3(10).Goshujin = g;
            new TestClass3(1).Goshujin = g;
            new TestClass3(2).Goshujin = g;
            new TestClass3(5).Goshujin = g;
            new TestClass3(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public TestGoshujin4 Initialize_LinkedList()
        {
            var g = new TestGoshujin4();
            new TestClass4(10).Goshujin = g;
            new TestClass4(1).Goshujin = g;
            new TestClass4(2).Goshujin = g;
            new TestClass4(5).Goshujin = g;
            new TestClass4(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public TestGoshujin5 Initialize_RawLinkedList()
        {
            var g = new TestGoshujin5();
            new TestClass5(10).Goshujin = g;
            new TestClass5(1).Goshujin = g;
            new TestClass5(2).Goshujin = g;
            new TestClass5(5).Goshujin = g;
            new TestClass5(3).Goshujin = g;

            return g;
        }

        [Benchmark]
        public TestGoshujin6 Initialize_StructLinkedList()
        {
            var g = new TestGoshujin6();
            new TestClass6(10).Goshujin = g;
            new TestClass6(1).Goshujin = g;
            new TestClass6(2).Goshujin = g;
            new TestClass6(5).Goshujin = g;
            new TestClass6(3).Goshujin = g;

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
        public int RemoveAdd_CrossLinkObsolete()
        {
            var c = this.goshujin.IdChain[4];
            this.goshujin.IdChain.Remove(c.IdLink);
            this.goshujin.IdChain.Add(c.IdLink);
            return this.goshujin.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_CrossLink1()
        {
            var c = this.goshujin1.IdChain[4];
            this.goshujin1.IdChain.Remove(c);
            this.goshujin1.IdChain.Add(c);
            return this.goshujin1.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_CrossLink2()
        {
            var c = this.goshujin2.IdChain[4];
            this.goshujin2.IdChain.Remove(c);
            this.goshujin2.IdChain.Add(c);
            return this.goshujin2.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_CrossLink3()
        {
            var c = this.goshujin3.IdChain[3];
            this.goshujin3.IdChain.Remove(c);
            this.goshujin3.IdChain.Add(c);
            return this.goshujin3.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_LinkedList()
        {
            var c = this.testClass43;
            this.goshujin4.IdChain.Remove(c);
            this.goshujin4.IdChain.AddLast(c);
            return this.goshujin4.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_RawLinkedList()
        {
            var c = this.testClass53;
            this.goshujin5.IdChain.Remove(c);
            this.goshujin5.IdChain.AddLast(c);
            return this.goshujin5.IdChain.Count;
        }

        [Benchmark]
        public int RemoveAdd_StructLinkedList()
        {
            var c = this.testClass63;
            this.goshujin6.IdChain.Remove(c);
            this.goshujin6.IdChain.AddLast(c);
            return this.goshujin6.IdChain.Count;
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
