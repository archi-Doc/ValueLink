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
        array.SequenceEqual([]);

        g.SlidingListChain.Resize(4);

        g.SlidingListChain.Set(0, g.IdChain.FindFirst(0)!);
        g.SlidingListChain.Set(1, g.IdChain.FindFirst(1)!);
        g.SlidingListChain.Set(2, g.IdChain.FindFirst(2)!);
        g.SlidingListChain.Set(3, g.IdChain.FindFirst(3)!);
        g.SlidingListChain.Set(4, g.IdChain.FindFirst(4)!);

        array = g.SlidingListChain.Select(x => x.Id).ToArray();
        array.SequenceEqual([0, 1, 2, 3]);

        var c = g.IdChain.FindFirst(0)!;
       c.Goshujin = null;
        g.SlidingListChain.Get(0).IsNull();
        g.SlidingListChain.StartPosition.Is(1);
        c.SlidingListLink.IsLinked.IsFalse();
    }
}
