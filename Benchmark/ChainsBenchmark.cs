using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using ValueLink;

namespace Benchmark;

[ValueLinkObject]
public partial record LinkedListTestClass
{
    [Link(Name = "LinkedList", Type = ChainType.LinkedList)]
    public LinkedListTestClass()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}

[Config(typeof(BenchmarkConfig))]
public class ChainsBenchmark
{
    private LinkedListTestClass.GoshujinClass g;
    private LinkedListTestClass c;

    public ChainsBenchmark()
    {
        this.g = new();
        this.c = new();
        this.c.Goshujin = this.g;
        new LinkedListTestClass().Goshujin = this.g;
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    /*[Benchmark]
    public LinkedListTestClass.GoshujinClass AddAndRemove()
    {
        this.g.LinkedListChain.Remove(c);
        this.g.LinkedListChain.AddLast(c);
        return this.g;
    }*/

    [Benchmark]
    public LinkedListTestClass.GoshujinClass AddLast()
    {
        this.g.LinkedListChain.AddLast(c);
        return this.g;
    }

    /*[Benchmark]
    public Arc.Collections.UnorderedLinkedList<LinkedListTestClass>.Node GetNode()
    {
        return this.g.LinkedListChain.GetNode(c);
    }*/
}
