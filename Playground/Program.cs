using System;
using Tinyhand;
using ValueLink;

namespace Playground;

[ValueLinkObject]
public partial class SharedChainTestClass
{
    [Link(Primary = true, Type = ChainType.Unordered)]
    public int PrimaryId { get; set; }

    [Link(Type = ChainType.Unordered)]
    public int SecondaryId { get; set; }

    public SharedChainTestClass(int primaryId, int secondaryId)
    {
        this.PrimaryId = primaryId;
        this.SecondaryId = secondaryId;
    }
}


internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var g = new SharedChainTestClass.GoshujinClass();
        var tc = new SharedChainTestClass(1, 2);
        var tc2 = new SharedChainTestClass(2, 3);

        g.Add(tc);
        g.Add(tc2);

        var c = g.PrimaryIdChain.FindFirst(0);
        c = g.PrimaryIdChain.FindFirst(1);
        c = g.PrimaryIdChain.FindFirst(2);
    }
}
