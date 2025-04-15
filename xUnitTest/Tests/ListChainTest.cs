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

        g.IdChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 1, 2, 3, 4 }).IsTrue();
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 1, 2, 3, 4 }).IsTrue();

        g.ListChain.RemoveAt(1);
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 2, 3, 4 }).IsTrue();

        var t = g.ListChain[2];
        g.ListChain.Remove(t);
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 2, 4 }).IsTrue();
        g.ListChain.Add(t);
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 2, 4, 3 }).IsTrue();

        t = g.ListChain[2];
        g.ListChain.Insert(1, t);
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 0, 4, 2, 3 }).IsTrue();
        g.ListChain[0] = g.ListChain[3];
        g.ListChain.Select(x => x.Id).SequenceEqual(new int[] { 3, 0, 4, 2 }).IsTrue();
    }
}
