// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
public partial class TargetChainTestClass
{
    [Link(Primary = true, Type = ChainType.Unordered)]
    public int PrimaryId { get; set; }

    [Link(UnsafeTargetChain = "PrimaryIdChain")]
    public int SecondaryId { get; set; }

    [Link(Type = ChainType.Unordered)]
    public string Name { get; set; } = string.Empty;

    public TargetChainTestClass(int primaryId, int secondaryId, string name)
    {
        this.PrimaryId = primaryId;
        this.SecondaryId = secondaryId;
        this.Name = name;
    }
}

public class SharedChainTest
{
    [Fact]
    public void Test1()
    {
        var g = new TargetChainTestClass.GoshujinClass();
        var tc = new TargetChainTestClass(1, 2, "A");
        var tc2 = new TargetChainTestClass(2, 3, "B");

        g.Add(tc);
        g.Add(tc2);

        var c = g.PrimaryIdChain.FindFirst(1)!;
        c.Name.Is("A");
        c = g.PrimaryIdChain.FindFirst(2)!;
        c.IsNotNull();

        c = g.PrimaryIdChain.FindFirst(3)!;
        c.Name.Is("B");

        c.Goshujin = default;

        g.PrimaryIdChain.FindFirst(3).IsNull();
        c = g.PrimaryIdChain.FindFirst(2)!;
        c.IsNotNull();
        c.Name.Is("A");

        c.Goshujin = default;
        g.Count.Is(0);
    }
}
