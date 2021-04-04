// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CrossLink;
using Tinyhand;
using Xunit;

namespace xUnitTest
{
    [CrossLinkObject]
    [TinyhandObject]
    public partial class TestClass1
    {
        [Link(Type = LinkType.Ordered)]
        [KeyAsName]
        private int id;

        [Link(Type = LinkType.Ordered)]
        [Key("NM")]
        private string name = default!;

        [Link(Type = LinkType.Ordered)]
        [KeyAsName]
        private byte age;

        [Link(Type = LinkType.StackList, Primary = true, Name = "Stack")]
        public TestClass1()
        {
        }

        public TestClass1(int id, string name, byte age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override bool Equals(object? obj)
        {
            var t = obj as TestClass1;
            if (t == null)
            {
                return false;
            }

            return this.id == t.id && this.name == t.name && this.age == t.age;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.id, this.name, this.age);
        }
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial class TestClass2
    {
        public int N { get; set; }

        [KeyAsName]
        public TestClass1.GoshujinClass G { get; set; } = default!;
    }

    public class BasicTest1
    {
        [Fact]
        public void Test1()
        {
            CrossLinkModule.Initialize();
            var g = new TestClass1.GoshujinClass();

            new TestClass1(0, "A", 100).Goshujin = g;
            new TestClass1(1, "Z", 12).Goshujin = g;
            new TestClass1(2, "1", 15).Goshujin = g;

            g.StackChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
            g.IdChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
            g.NameChain.Select(x => x.Id).SequenceEqual(new int[] { 2, 0, 1 }).IsTrue();
            g.AgeChain.Select(x => x.Id).SequenceEqual(new int[] { 1, 2, 0 }).IsTrue();

            var st = TinyhandSerializer.SerializeToString(g);
            var g2 = TinyhandSerializer.Deserialize<TestClass1.GoshujinClass>(TinyhandSerializer.Serialize(g));

            var tc2 = new TestClass2();
            tc2.N = 100;
            tc2.G = g;
            st = TinyhandSerializer.SerializeToString(tc2);
            var tc2a = TinyhandSerializer.Deserialize<TestClass2>(TinyhandSerializer.Serialize(tc2))!;
            var tc2b = TinyhandSerializer.Reconstruct<TestClass2>();

            g2!.StackChain.SequenceEqual(g.StackChain).IsTrue();
            g2!.IdChain.SequenceEqual(g.IdChain).IsTrue();
            g2!.NameChain.SequenceEqual(g.NameChain).IsTrue();
            g2!.AgeChain.SequenceEqual(g.AgeChain).IsTrue();

            tc2a.G.StackChain.SequenceEqual(g.StackChain).IsTrue();
            tc2a.G.IdChain.SequenceEqual(g.IdChain).IsTrue();
            tc2a.G.NameChain.SequenceEqual(g.NameChain).IsTrue();
            tc2a.G.AgeChain.SequenceEqual(g.AgeChain).IsTrue();
        }
    }
}
