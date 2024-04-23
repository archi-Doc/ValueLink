﻿using System;
using Tinyhand;
using ValueLink;

namespace Playground;

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

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var g = new TargetChainTestClass.GoshujinClass();
        var tc = new TargetChainTestClass(1, 2, "A");
        var tc2 = new TargetChainTestClass(2, 3, "B");

        g.Add(tc);
        g.Add(tc2);

        var c = g.PrimaryIdChain.FindFirst(0);
        c = g.PrimaryIdChain.FindFirst(1)!;
        // c = g.PrimaryIdChain.FindFirst(2)!;
        var c2 = g.PrimaryIdChain.FindFirst(3);

        c.Goshujin = default;
        c = g.PrimaryIdChain.FindFirst(2);
    }
}
