// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
public partial class ListChainTestClass
{
    public partial class GoshujinClass
    {
        public int[] GetArray()
            => this.ListChain.Select(x => x.Id).ToArray();

        public bool SequenceEqual(IEnumerable<int> e)
            => this.ListChain.Select(x => x.Id).SequenceEqual(e);
    }

    [Link(Type = ChainType.Ordered)]
    public partial int Id { get; set; }

    [Link(Type = ChainType.List, Name = "List")]
    public ListChainTestClass(int id)
    {
        this.Id = id;
    }

    public partial GoshujinClass? Goshujin { get; set; }

    public override string ToString() => this.Id.ToString();
}

[ValueLinkObject]
public partial class ListChainTestClass2
{
    public partial class GoshujinClass
    {
        public int[] GetArray()
            => this.ListChain.Select(x => x.Id).ToArray();

        public bool SequenceEqual(IEnumerable<int> e)
            => this.ListChain.Select(x => x.Id).SequenceEqual(e);
    }

    [Link(Primary = true, Type = ChainType.Ordered)]
    public partial int Id { get; set; }

    [Link(Type = ChainType.List, Name = "List", AutoLink = false)]
    public ListChainTestClass2(int id)
    {
        this.Id = id;
    }

    public override string ToString() => this.Id.ToString();
}

public class ListChainTest
{
    [Fact]
    public void Test2()
    {
        var g = new ListChainTestClass2.GoshujinClass();
        var c0 = new ListChainTestClass2(0);
        c0.Goshujin = g;
        g.ListChain.Add(c0);
        var c1 = new ListChainTestClass2(1);
        c1.Goshujin = g;

        var array = g.ToArray();
        array = g.ListChain.ToArray();
    }

    [Fact]
    public void Test1()
    {
        var g = new ListChainTestClass.GoshujinClass();

        new ListChainTestClass(0).Goshujin = g;
        new ListChainTestClass(1).Goshujin = g;
        new ListChainTestClass(2).Goshujin = g;
        new ListChainTestClass(3).Goshujin = g;
        new ListChainTestClass(4).Goshujin = g;
        var (c0, c1, c2, c3, c4) = (g.ListChain[0], g.ListChain[1], g.ListChain[2], g.ListChain[3], g.ListChain[4]);

        g.ListChain.Select(x => x.Id).SequenceEqual([0, 1, 2, 3, 4]).IsTrue();
        g.GetArray().SequenceEqual([0, 1, 2, 3, 4]).IsTrue();
        c3.Goshujin = default;
        c3.ListLink.Index.Is(-1);
        g.SequenceEqual([0, 1, 2, 4]).IsTrue();
        c4.ListLink.Index.Is(3);
        c4.Goshujin = default;
        c4.ListLink.Index.Is(-1);
        g.SequenceEqual([0, 1, 2]).IsTrue();
        c3.Goshujin = g;
        c4.Goshujin = g;

        g.SequenceEqual([0, 1, 2, 3, 4]).IsTrue();

        g.ListChain.RemoveAt(1);
        g.SequenceEqual([0, 4, 2, 3,]).IsTrue();

        g.ListChain.Remove(c3);
        g.SequenceEqual([0, 4, 2]).IsTrue();
        g.ListChain.Add(c2);
        g.SequenceEqual([0, 4, 2]).IsTrue();

        g.ListChain.Add(c0);
        g.SequenceEqual([2, 4, 0,]).IsTrue();
        g.ListChain.Add(c0);
        g.SequenceEqual([2, 4, 0,]).IsTrue();
        g.ListChain.Add(c3);
        g.SequenceEqual([2, 4, 0, 3,]).IsTrue();

        g.ListChain.Insert(1, c1);
        g.SequenceEqual([2, 1, 0, 3, 4,]).IsTrue();
        g.ListChain.Insert(0, c2);
        g.SequenceEqual([2, 1, 0, 3, 4,]).IsTrue();

        g.ListChain.Remove(c1);
        g.SequenceEqual([2, 4, 0, 3,]).IsTrue();
        g.ListChain.Insert(4, c1);
        g.SequenceEqual([2, 4, 0, 3, 1]).IsTrue();
    }

    /*public void Test1()
    {
        var g = new ListChainTestClass.GoshujinClass();

        new ListChainTestClass(0).Goshujin = g;
        new ListChainTestClass(1).Goshujin = g;
        new ListChainTestClass(2).Goshujin = g;
        new ListChainTestClass(3).Goshujin = g;
        new ListChainTestClass(4).Goshujin = g;

        g.ListChain.Select(x => x.Id).SequenceEqual([0, 1, 2, 3, 4]).IsTrue();
        var c3 = g.ListChain[3]!;
        var c4 = g.ListChain[4]!;
        c3.Goshujin = default;
        c3.ListLink.Index.Is(-1);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 1, 2, 4]).IsTrue();
        c4.ListLink.Index.Is(3);
        c4.Goshujin = default;
        c4.ListLink.Index.Is(-1);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 1, 2]).IsTrue();
        c3.Goshujin = g;
        c4.Goshujin = g;

        g.IdChain.Select(x => x.Id).SequenceEqual([0, 1, 2, 3, 4]).IsTrue();
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 1, 2, 3, 4]).IsTrue();

        g.ListChain.RemoveAt(1);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 2, 3, 4]).IsTrue();

        var t = g.ListChain[2];
        g.ListChain.Remove(t);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 2, 4]).IsTrue();
        g.ListChain.Add(t);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 2, 4, 3]).IsTrue();

        t = g.ListChain[2];
        g.ListChain.Insert(1, t);
        g.ListChain.Select(x => x.Id).SequenceEqual([0, 4, 2, 3]).IsTrue();
        g.ListChain[0] = g.ListChain[3];
        g.ListChain.Select(x => x.Id).SequenceEqual([3, 0, 4, 2]).IsTrue();
    }*/
}

public class ListChainInsertTest
{
    [Fact]
    public void Insert_RelinkingAtTheEnd_KeepsTheChainConsistent()
    {
        var g = new ListChainTestClass.GoshujinClass();
        for (var i = 0; i < 5; i++)
        {
            new ListChainTestClass(i).Goshujin = g;
        }

        var chain = g.ListChain;
        chain.Count.Is(5);

        // Re-insert an already-linked object at index == Count.
        var obj = chain[0];
        chain.Insert(chain.Count, obj);

        chain.Count.Is(5);
        chain.IndexOf(obj).Is(4);
        chain[4].Is(obj);
        for (var i = 0; i < chain.Count; i++)
        {
            chain[i].IsNotNull();
            chain.IndexOf(chain[i]).Is(i);
        }

        chain.ToArray().Length.Is(5);
    }

    [Fact]
    public void Indexer_RejectsIndexesPastCount()
    {
        var g = new ListChainTestClass.GoshujinClass();
        new ListChainTestClass(0).Goshujin = g;

        var chain = g.ListChain;
        chain.Count.Is(1);

        // The backing array starts at capacity 4, so index 1..3 used to read stale slots.
        Assert.Throws<ArgumentOutOfRangeException>(() => chain[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => chain[-1]);
    }
}

public class ListChainIndexerTest
{
    private static ListChainTestClass.GoshujinClass CreateGoshujin(int count)
    {
        var g = new ListChainTestClass.GoshujinClass();
        for (var i = 0; i < count; i++)
        {
            new ListChainTestClass(i).Goshujin = g;
        }

        return g;
    }

    private static void AssertConsistent(ListChain<ListChainTestClass> chain)
    {
        for (var i = 0; i < chain.Count; i++)
        {
            chain[i].IsNotNull();
            chain.IndexOf(chain[i]).Is(i);
            chain[i].ListLink.IsLinked.IsTrue();
        }
    }

    [Fact]
    public void Set_ReplacesTheElementInPlace()
    {
        var g = CreateGoshujin(5);
        var chain = g.ListChain;
        var replaced = chain[2];

        // A fresh object that belongs to the Goshujin but is not in the List chain.
        var fresh = new ListChainTestClass(99) { Goshujin = g, };
        chain.Remove(fresh);
        chain.Count.Is(5);

        chain[2] = fresh;

        chain.Count.Is(5); // Replace, not insert.
        chain[2].Is(fresh);
        chain.IndexOf(fresh).Is(2);
        replaced.ListLink.IsLinked.IsFalse();
        chain.Contains(replaced).IsFalse();
        AssertConsistent(chain);
    }

    [Fact]
    public void Set_WithTheSameObject_IsANoOp()
    {
        var g = CreateGoshujin(3);
        var chain = g.ListChain;
        var obj = chain[1];

        chain[1] = obj;

        chain.Count.Is(3);
        chain[1].Is(obj);
        AssertConsistent(chain);
    }

    [Fact]
    public void Set_WithAnObjectAlreadyInTheChain_MovesItAndShrinks()
    {
        var g = CreateGoshujin(5);
        var chain = g.ListChain;
        var moved = chain[0];
        var replaced = chain[3];

        chain[3] = moved;

        chain.Count.Is(4); // Two slots collapsed into one.
        replaced.ListLink.IsLinked.IsFalse();
        moved.ListLink.IsLinked.IsTrue();
        chain.Contains(moved).IsTrue();
        chain.Contains(replaced).IsFalse();
        AssertConsistent(chain);
    }

    [Fact]
    public void Set_WithAnObjectAtTheLastSlot_StaysConsistent()
    {
        var g = CreateGoshujin(5);
        var chain = g.ListChain;
        var moved = chain[4];
        var replaced = chain[1];

        chain[1] = moved;

        chain.Count.Is(4);
        chain[1].Is(moved);
        replaced.ListLink.IsLinked.IsFalse();
        AssertConsistent(chain);
    }

    [Fact]
    public void Set_RejectsOutOfRangeIndexes()
    {
        var g = CreateGoshujin(2);
        var chain = g.ListChain;
        var fresh = new ListChainTestClass(99) { Goshujin = g, };
        chain.Remove(fresh);

        Assert.Throws<ArgumentOutOfRangeException>(() => chain[2] = fresh);
        Assert.Throws<ArgumentOutOfRangeException>(() => chain[-1] = fresh);
        chain.Count.Is(2);
    }

    [Fact]
    public void Set_RejectsObjectsFromAnotherGoshujin()
    {
        var g = CreateGoshujin(2);
        var other = new ListChainTestClass(99) { Goshujin = new ListChainTestClass.GoshujinClass(), };

        Assert.Throws<UnmatchedGoshujinException>(() => g.ListChain[0] = other);
    }
}
