// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ValueLink;
using Tinyhand;

namespace Benchmark.Serializer
{
    [ValueLinkObject]
    [TinyhandObject]
    public partial class TestClass
    {
        [Link(Name = "Stack", Primary = true, Type = ChainType.StackList)]
        public TestClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public TestClass()
        {
        }

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private int id;

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private string name = default!;
    }

    [Config(typeof(BenchmarkConfig))]
    public class SerializerBenchmark
    {
        [Params(100)]
        public int Length;

        public SerializerBaseClass.GoshujinClass BaseGoshujin = default!;

        public byte[] BaseByte = default!;

        public TestClass.GoshujinClass TestGoshujin = default!;

        public byte[] TestByte = default!;

        public SerializerBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.BaseGoshujin = new();
            for (var n = 0; n < this.Length; n++)
            {
                new SerializerBaseClass((n * 13) % 100, n.ToString()).Goshujin = this.BaseGoshujin;
            }

            this.BaseByte = TinyhandSerializer.Serialize(this.BaseGoshujin);
            var st = TinyhandSerializer.SerializeToString(this.BaseGoshujin);

            this.TestGoshujin = new();
            for (var n = 0; n < this.Length; n++)
            {
                new TestClass((n * 13) % 100, n.ToString()).Goshujin = this.TestGoshujin;
            }

            this.TestByte = TinyhandSerializer.Serialize(this.TestGoshujin);
            var st2 = TinyhandSerializer.SerializeToString(this.TestGoshujin);
        }

        [Benchmark]
        public byte[] Serialize_Base()
        {
            return TinyhandSerializer.Serialize(this.BaseGoshujin);
        }

        [Benchmark]
        public byte[] Serialize_ValueLink()
        {
            return TinyhandSerializer.Serialize(this.TestGoshujin);
        }

        [Benchmark]
        public SerializerBaseClass.GoshujinClass? Deserialize_Base()
        {
            return TinyhandSerializer.Deserialize< SerializerBaseClass.GoshujinClass>(this.BaseByte);
        }

        [Benchmark]
        public TestClass.GoshujinClass? Deserialize_ValueLink()
        {
            return TinyhandSerializer.Deserialize<TestClass.GoshujinClass>(this.TestByte);
        }
    }
}
