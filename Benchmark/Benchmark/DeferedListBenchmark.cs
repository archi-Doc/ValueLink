// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Benchmark;
using BenchmarkDotNet.Attributes;
using ValueLink;

namespace Benchmark;

[ValueLinkObject]
public partial record DeferedTestClass
{
    public DeferedTestClass()
    {
    }

    public DeferedTestClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Link(Primary = true, Type = ChainType.Unordered)]
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}

[Config(typeof(BenchmarkConfig))]
public class DeferedListBenchmark
{
    private readonly DeferedTestClass.GoshujinClass g = new();

    public DeferedListBenchmark()
    {
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass List_AddAndRemove()
    {
        var d = new DeferredList<DeferedTestClass.GoshujinClass, DeferedTestClass>(this.g);
        d.Add(new(1, "a"));
        d.Add(new(2, "b"));
        d.Add(new(10, "c"));
        d.Add(new(100, "d"));
        d.DeferredAdd();

        foreach (var x in this.g)
        {
            d.Add(x);
        }

        d.DeferredRemove();
        return g;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass Queue_AddAndRemove()
    {
        var d = new DeferredQueue<DeferedTestClass.GoshujinClass, DeferedTestClass>(this.g);
        d.Add(new(1, "a"));
        d.Add(new(2, "b"));
        d.Add(new(10, "c"));
        d.Add(new(100, "d"));
        d.DeferredAdd();

        foreach (var x in this.g)
        {
            d.Add(x);
        }

        d.DeferredRemove();
        return g;
    }
}
