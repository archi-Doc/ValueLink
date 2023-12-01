// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
public partial class SlidingListChainClass
{
    [Link(Type = ChainType.Ordered)]
    public int Id { get; set; }

    [Link(Type = ChainType.SlidingList, Name = "SlidingList")]
    public SlidingListChainClass(int id)
    {
        this.Id = id;
    }

    public override string ToString() => this.Id.ToString();
}

public class SlidingListChainTest
{
    [Fact]
    public void Test1()
    {
        var g = new SlidingListChainClass.GoshujinClass();
        g.SlidingListChain.Resize(4);

        new SlidingListChainClass(0).Goshujin = g;
        new SlidingListChainClass(1).Goshujin = g;
        new SlidingListChainClass(3).Goshujin = g;
        new SlidingListChainClass(2).Goshujin = g;
        new SlidingListChainClass(4).Goshujin = g;

        var array = g.SlidingListChain.Select(x => x.Id).ToArray();
        array.SequenceEqual([0, 1, 3, 2]);
    }
}
