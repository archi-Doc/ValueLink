// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;
using Arc.Collections;

namespace xUnitTest;

// [TinyhandObject]
// [ValueLinkObject] // Error!
public partial class TestClass1Derived : TestClass1
{
    public void Test()
    {
        // this.HeightValue = 1d;
    }
}

[ValueLinkObject]
[TinyhandObject]
public partial class TestClass1 : IComparable<TestClass1>
{
    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private int Id;

    [Link(Type = ChainType.Ordered)]
    [Key("NM")]
    private string? Name = default!;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private byte Age;

    /*[Link(Type = ChainType.Ordered)]
    [KeyAsName]
    public double Height { get; } // Error! */

    [Link(Type = ChainType.StackList, Primary = true, Name = "Stack")]
    public TestClass1()
    {
    }

    public TestClass1(int id, string name, byte age)
    {
        this.Id = id;
        this.Name = name;
        this.Age = age;
    }

    public int CompareTo(TestClass1? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (this.Id < other.Id)
        {
            return -1;
        }
        else if (this.Id > other.Id)
        {
            return 1;
        }

        return 0;
    }

    public override bool Equals(object? obj)
    {
        var t = obj as TestClass1;
        if (t == null)
        {
            return false;
        }

        return this.Id == t.Id && this.Name == t.Name && this.Age == t.Age;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Id, this.Name, this.Age);
    }
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class TestClass2
{
    public int N { get; set; }

    [KeyAsName]
    public TestClass1.GoshujinClass G { get; set; } = default!;
}

[ValueLinkObject]
[TinyhandObject]
public partial class GenericTestClass<T>
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [KeyAsName]
    private int id;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private T value = default!;

    public GenericTestClass()
    {
    }

    public GenericTestClass(int id, T value)
    {
        this.id = id;
        this.value = value;
    }

    public override bool Equals(object? obj)
    {
        var t = obj as GenericTestClass<T>;
        if (t == null)
        {
            return false;
        }

        return this.id == t.id && EqualityComparer<T>.Default.Equals(this.value, t.value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.id, this.value);
    }
}

[ValueLinkObject]
[TinyhandObject]
public partial class GenericTestClass2<T>
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [KeyAsName]
    private int Id;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private T Value = default!;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private NestedClass<double, int> Nested = default!;

    public GenericTestClass2()
    {
    }

    public GenericTestClass2(int id, T value, NestedClass<double, int> nested)
    {
        this.Id = id;
        this.Value = value;
        this.Nested = nested;
    }

    public override bool Equals(object? obj)
    {
        var t = obj as GenericTestClass2<T>;
        if (t == null)
        {
            return false;
        }

        return this.Id == t.Id && EqualityComparer<T>.Default.Equals(this.Value, t.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Id, this.Value);
    }

    [ValueLinkObject]
    [TinyhandObject]
    public partial class NestedClass<X, Y> : IComparable<NestedClass<X, Y>>
    {
        [Link(Type = ChainType.Ordered, Primary = true)]
        [KeyAsName]
        private string name = default!;

        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private X xvalue = default!;

        [KeyAsName]
        private Y yvalue = default!;

        public NestedClass()
        {
        }

        public NestedClass(string name, X xvalue, Y yvalue)
        {
            this.name = name;
            this.xvalue = xvalue;
            this.yvalue = yvalue;
        }

        public int CompareTo(NestedClass<X, Y>? other)
        {
            if (other == null)
            {
                return 1;
            }

            return this.name.CompareTo(other.name);
        }
    }

    [ValueLinkObject]
    [TinyhandObject]
    public partial class NestedClass2
    {
        [Link(Type = ChainType.Ordered, Primary = true)]
        [KeyAsName]
        private double height;

        public NestedClass2()
        {
        }

        public NestedClass2(double height)
        {
            this.height = height;
        }
    }
}

public class BasicTest1
{
    [Fact]
    public void Test1()
    {
        // ValueLinkModule.Initialize();
        var g = new TestClass1.GoshujinClass();

        new TestClass1(0, "A", 100).Goshujin = g;
        new TestClass1(1, "Z", 12).Goshujin = g;
        new TestClass1(2, "1", 15).Goshujin = g;

        g.StackChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
        g.IdChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();
        g.NameChain.Select(x => x.IdValue).SequenceEqual(new int[] { 2, 0, 1 }).IsTrue();
        g.AgeChain.Select(x => x.IdValue).SequenceEqual(new int[] { 1, 2, 0 }).IsTrue();

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

        tc2a = TinyhandSerializer.Clone(tc2);

        tc2a.G.StackChain.SequenceEqual(g.StackChain).IsTrue();
        tc2a.G.IdChain.SequenceEqual(g.IdChain).IsTrue();
        tc2a.G.NameChain.SequenceEqual(g.NameChain).IsTrue();
        tc2a.G.AgeChain.SequenceEqual(g.AgeChain).IsTrue();

        g.IdChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 1, 2 }).IsTrue();

        var i = g.IdChain.FindFirst(1);
        i.IsNotNull();
        var queue = new TemporaryQueue<TestClass1>();
        queue.Enqueue(i!);
        g.Remove(ref queue);

        g.IdChain.Select(x => x.IdValue).SequenceEqual(new int[] { 0, 2 }).IsTrue();
    }

    [Fact]
    public void TestGeneric()
    {
        string st;

        var g = new GenericTestClass<int>.GoshujinClass();
        new GenericTestClass<int>(0, 1).Goshujin = g;
        new GenericTestClass<int>(2, 3).Goshujin = g;

        st = TinyhandSerializer.SerializeToString(g);
        g = TinyhandSerializer.Deserialize<GenericTestClass<int>.GoshujinClass>(TinyhandSerializer.Serialize(g));

        var g2 = new GenericTestClass<TestClass1>.GoshujinClass();
        new GenericTestClass<TestClass1>(0, new TestClass1(1, "1", 10)).Goshujin = g2;
        new GenericTestClass<TestClass1>(2, new TestClass1(2, "2", 20)).Goshujin = g2;

        st = TinyhandSerializer.SerializeToString(g2);
        g2 = TinyhandSerializer.Deserialize<GenericTestClass<TestClass1>.GoshujinClass>(TinyhandSerializer.Serialize(g2));

        var g3 = new GenericTestClass2<TestClass1>.GoshujinClass();
        new GenericTestClass2<TestClass1>(0, new TestClass1(1, "1", 10), new GenericTestClass2<TestClass1>.NestedClass<double, int>("a", 1.2, 100)).Goshujin = g3;
        new GenericTestClass2<TestClass1>(2, new TestClass1(2, "2", 20), new GenericTestClass2<TestClass1>.NestedClass<double, int>("1", 1, 2)).Goshujin = g3;

        st = TinyhandSerializer.SerializeToString(g3);
        g3 = TinyhandSerializer.Deserialize<GenericTestClass2<TestClass1>.GoshujinClass>(TinyhandSerializer.Serialize(g3));

        var gg = new GenericTestClass2<TestClass1>.NestedClass<double, int>.GoshujinClass();
        foreach (var x in g3!.IdChain)
        {
            x.NestedValue.Goshujin = gg;
        }

        st = TinyhandSerializer.SerializeToString(gg);
        gg = TinyhandSerializer.Deserialize<GenericTestClass2<TestClass1>.NestedClass<double, int>.GoshujinClass>(TinyhandSerializer.Serialize(gg));
    }
}
