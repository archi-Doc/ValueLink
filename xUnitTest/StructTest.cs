// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest
{
    [ValueLinkObject]
    [TinyhandObject]
    public partial record TestStruct : IEquatable<TestStruct>
    {
        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private int Id;

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private string Name = default!;

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private byte Age;

        [Link(Type = ChainType.StackList, Primary = true, Name = "Stack")]
        public TestStruct(int id, string name, byte age)
        {
            this.Id = id;
            this.Name = name;
            this.Age = age;
        }

        public virtual bool Equals(TestStruct? obj)
        {// Custom Equals() required because the default implementation of Equals() includes generated members (e.g. Goshujin). 
            if (obj == null)
            {
                return false;
            }

            return this.Id == obj.Id && this.Name == obj.Name && this.Age == obj.Age;
        }

        public override int GetHashCode() => HashCode.Combine(this.Id, this, Name, this.Age);
    }

    public class StructTest
    {
        [Fact]
        public void Test1()
        {
            var g = new TestStruct.GoshujinClass();

            new TestStruct(0, "A", 100).Goshujin = g;
            new TestStruct(1, "Z", 12).Goshujin = g;
            new TestStruct(2, "1", 15).Goshujin = g;

            g.StackChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
            g.IdChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
            g.NameChain.Select(x => x.IdValue).SequenceEqual(new int[] { 2, 0, 1 }).IsTrue();
            g.AgeChain.Select(x => x.IdValue).SequenceEqual(new int[] { 1, 2, 0 }).IsTrue();

            var st = TinyhandSerializer.SerializeToString(g);
            var g2 = TinyhandSerializer.Deserialize<TestStruct.GoshujinClass>(TinyhandSerializer.Serialize(g))!;

            g2.StackChain.SequenceEqual(g.StackChain).IsTrue();
            g2.IdChain.SequenceEqual(g.IdChain).IsTrue();
            g2.NameChain.SequenceEqual(g.NameChain).IsTrue();
            g2.AgeChain.SequenceEqual(g.AgeChain).IsTrue();

        }
    }
}
