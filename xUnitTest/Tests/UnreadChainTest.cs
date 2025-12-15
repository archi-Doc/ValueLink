// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject]
public partial class UnreadChainTestClass
{// For versioning
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { get; set; }

    [Key(1)]
    [Link(Type = ChainType.Ordered)]
    public int Id2 { get; set; }

    [Key(2)]
    [Link(Type = ChainType.Ordered, AutoLink = false)]
    public int Id3 { get; set; }

    public UnreadChainTestClass()
    {
    }

    public UnreadChainTestClass(int id)
    {
        this.Id = id;
        this.Id2 = id;
        this.Id3 = id;
    }
}

[TinyhandObject]
[ValueLinkObject]
public partial class UnreadChainTestClass2
{// For versioning
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { get; set; }

    [Key(1)]
    // [Link(Type = ChainType.Ordered)]
    public int Id2 { get; set; }

    [Key(2)]
    // [Link(Type = ChainType.Ordered, AutoLink = false)]
    public int Id3 { get; set; }

    public UnreadChainTestClass2()
    {
    }

    public UnreadChainTestClass2(int id)
    {
        this.Id = id;
        this.Id2 = id;
        this.Id3 = id;
    }
}

public class UnreadChainTest
{
    [Fact]
    public void Test1()
    {
        var g = new UnreadChainTestClass.GoshujinClass();

        new UnreadChainTestClass(1).Goshujin = g;
        new UnreadChainTestClass(2).Goshujin = g;
        new UnreadChainTestClass(-2).Goshujin = g;
        new UnreadChainTestClass(3).Goshujin = g;

        g.IdChain.Select(x => x.Id).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g.Id2Chain.Select(x => x.Id2).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g.Id3Chain.Select(x => x.Id3).SequenceEqual([]).IsTrue();

        foreach (var x in g)
        {// Manual link
            g.Id3Chain.Add(x.Id3, x);
        }

        g.Id3Chain.Select(x => x.Id3).SequenceEqual([-2, 1, 2, 3,]).IsTrue();

        // g -> g2
        var g2 = TinyhandSerializer.Deserialize<UnreadChainTestClass.GoshujinClass>(TinyhandSerializer.Serialize(g));
        g2!.IdChain.Select(x => x.Id).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g2!.Id2Chain.Select(x => x.Id2).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g2!.Id3Chain.Select(x => x.Id3).SequenceEqual([-2, 1, 2, 3,]).IsTrue();

        // g(UnreadChainTestClass) -> g3(UnreadChainTestClass2)
        var g3 = TinyhandSerializer.Deserialize<UnreadChainTestClass2.GoshujinClass>(TinyhandSerializer.Serialize(g));
        g3!.IdChain.Select(x => x.Id).SequenceEqual([-2, 1, 2, 3,]).IsTrue();

        // g3(UnreadChainTestClass2) -> g2(UnreadChainTestClass)
        g2 = TinyhandSerializer.Deserialize<UnreadChainTestClass.GoshujinClass>(TinyhandSerializer.Serialize(g3));
        g2!.IdChain.Select(x => x.Id).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g2!.Id2Chain.Select(x => x.Id2).SequenceEqual([-2, 1, 2, 3,]).IsTrue();
        g2!.Id3Chain.Select(x => x.Id3).SequenceEqual([]).IsTrue();
    }
}
