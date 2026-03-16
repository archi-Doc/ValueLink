// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
public partial class ClearTestClass
{
    public partial class GoshujinClass
    {
        public int[] GetArray()
            => this.IdChain.Select(x => x.Id).ToArray();
    }

    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { get; set; }

    public ClearTestClass(int id)
    {
        this.Id = id;
    }
}

public class ClearTest
{
    [Fact]
    public void Test1()
    {
        var g = new ClearTestClass.GoshujinClass();
        var tc1 = new ClearTestClass(1);
        tc1.Goshujin = g;
        var tc2 = new ClearTestClass(2);
        tc2.Goshujin = g;
        var tc3 = new ClearTestClass(3);
        tc3.Goshujin = g;

        g.GetArray().SequenceEqual([1, 2, 3,]);
        g.ClearChains();
        g.GetArray().SequenceEqual([]);
        tc1.Goshujin.Is(g);
        tc2.Goshujin.Is(g);
        tc3.Goshujin.Is(g);
    }

    [Fact]
    public void Test2()
    {
        var g = new ClearTestClass.GoshujinClass();
        var tc1 = new ClearTestClass(1);
        tc1.Goshujin = g;
        var tc2 = new ClearTestClass(2);
        tc2.Goshujin = g;
        var tc3 = new ClearTestClass(3);
        tc3.Goshujin = g;

        g.GetArray().SequenceEqual([1, 2, 3,]);
        g.ClearAll();
        g.GetArray().SequenceEqual([]);
        tc1.Goshujin.Is(null);
        tc2.Goshujin.Is(null);
        tc3.Goshujin.Is(null);
    }
}
