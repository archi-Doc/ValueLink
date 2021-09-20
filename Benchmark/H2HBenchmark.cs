using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using BenchmarkDotNet.Attributes;
using CrossLink;

namespace Benchmark
{
    public class H2HClass
    {
        public H2HClass(int id)
        {
            this.Id = id;
        }

        public int Id { get; }
    }

    [CrossLinkObject]
    public partial class H2HClass2
    {
        public H2HClass2(int id)
        {
            this.id = id;
        }

        [Link(Type = ChainType.Ordered)]
        private int id;
    }

    [Config(typeof(BenchmarkConfig))]
    public class H2HBenchmark
    {
        public const int N = 10;

        [Params(100)]
        public int Length;

        public int[] IntArray = default!;

        public H2HClass[] H2HList = default!;

        public H2HClass2[] H2HList2 = default!;

        public SortedDictionary<int, H2HClass> H2HSortedDictionary = default!;

        public H2HClass2.GoshujinClass H2HGoshujin = default!;

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = BenchmarkHelper.GetUniqueRandomNumbers(r, -Length, +Length, Length).ToArray();

            this.H2HList = new H2HClass[this.IntArray.Length];
            this.H2HSortedDictionary = new();
            for (var n = 0; n < this.IntArray.Length; n++)
            {
                this.H2HList[n] = new H2HClass(this.IntArray[n]);
                this.H2HSortedDictionary.Add(this.IntArray[n], this.H2HList[n]);
            }

            this.H2HList2 = new H2HClass2[this.IntArray.Length];
            this.H2HGoshujin = new();
            for (var n = 0; n < this.IntArray.Length; n++)
            {
                this.H2HList2[n] = new H2HClass2(this.IntArray[n]);
                this.H2HList2[n].Goshujin = this.H2HGoshujin;
            }

            var node = this.H2HGoshujin.IdChain.First;
            foreach (var x in this.H2HSortedDictionary.Values)
            {
                Debug.Assert(x.Id == node!.Id);
                node = node.IdLink.Next;
            }
        }

        [Benchmark]
        public int NewAndAdd_SortedDictionary()
        {
            var g = new SortedDictionary<int, H2HClass>();
            foreach (var x in this.IntArray)
            {
                g.Add(x, new H2HClass(x));
            }

            return g.Count;
        }

        [Benchmark]
        public int NewAndAdd_CrossLink()
        {
            var g = new H2HClass2.GoshujinClass();
            foreach (var x in this.IntArray)
            {
                new H2HClass2(x).Goshujin = g;
            }

            return g.IdChain.Count;
        }

        /*[Benchmark]
        public int NewAndAdd_OrderedMultiMap()
        {
            var g = new OrderedMultiMap<int, H2HClass>();
            foreach (var x in this.IntArray)
            {
                g.Add(x, new H2HClass(x));
            }

            return g.Count;
        }*/

        [Benchmark]
        public int RemoveAndAdd_SortedDictionary()
        {
            for (var n = 0; n < N; n++)
            {
                this.H2HSortedDictionary.Remove(this.H2HList[n].Id);
            }

            for (var n = 0; n < N; n++)
            {
                this.H2HSortedDictionary.Add(this.H2HList[n].Id, this.H2HList[n]);
            }

            return this.H2HSortedDictionary.Count;
        }

        [Benchmark]
        public int RemoveAndAdd_CrossLink()
        {
            for (var n = 0; n < N; n++)
            {
                this.H2HGoshujin.IdChain.Remove(this.H2HList2[n]);
            }

            for (var n = 0; n < N; n++)
            {
                this.H2HGoshujin.IdChain.Add(this.H2HList2[n].Id, this.H2HList2[n]);
            }

            return this.H2HGoshujin.IdChain.Count;
        }
    }
}
