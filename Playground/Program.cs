using System;
using Tinyhand;
using ValueLink;

namespace Playground;

[ValueLinkObject]
public partial class SharedChainTestClass
{
    [Link(Primary = true, Type = ChainType.Unordered)]
    public int PrimaryId { get; set; }

    [Link(SharedChain = "NameChain")]
    public int SecondaryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public SharedChainTestClass(int primaryId, int secondaryId, string name)
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

        var g = new SharedChainTestClass.GoshujinClass();
        var tc = new SharedChainTestClass(1, 2, "A");
        var tc2 = new SharedChainTestClass(2, 3, "B");

        g.Add(tc);
        g.Add(tc2);

        var c = g.PrimaryIdChain.FindFirst(0);
        c = g.PrimaryIdChain.FindFirst(1);
        c = g.PrimaryIdChain.FindFirst(2)!;
        var c2 = g.PrimaryIdChain.FindFirst(3);

        c.Goshujin = default;
        c = g.PrimaryIdChain.FindFirst(2);
    }
}
