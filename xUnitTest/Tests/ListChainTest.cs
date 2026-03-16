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

    public override string ToString() => this.Id.ToString();
}

public class ListChainTest
{
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
