// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using BenchmarkDotNet.Attributes;
using Tinyhand.Tree;
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
    private readonly DeferedTestClass.GoshujinClass goshujin = new();

    public DeferedListBenchmark()
    {
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass DeferredList_ForEach()
    {
        var d = new DeferredList<DeferedTestClass.GoshujinClass, DeferedTestClass>(this.goshujin);
        d.Add(new(1, "a"));
        d.Add(new(2, "b"));
        d.Add(new(10, "c"));
        d.Add(new(100, "d"));
        d.DeferredAdd();

        foreach (var x in this.goshujin)
        {
            d.Add(x);
        }

        d.DeferredRemove();
        return goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryList_ForEach()
    {
        var list = default(TemporaryList<DeferedTestClass>);
        list.Add(new(1, "a"));
        list.Add(new(2, "b"));
        list.Add(new(10, "c"));
        list.Add(new(100, "d"));

        foreach (var x in list)
        {
            x.Goshujin = this.goshujin;
            // this.goshujin.Add(x);
        }

        foreach (var x in list)
        {
            x.Goshujin = default;
            // this.goshujin.Remove(x);
        }

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryList_AddToGoshujin()
    {
        var list = default(TemporaryList<DeferedTestClass>);
        list.Add(new(1, "a"));
        list.Add(new(2, "b"));
        list.Add(new(10, "c"));
        list.Add(new(100, "d"));

        list.AddToGoshujin(this.goshujin);
        list.RemoveFromGoshujin<DeferedTestClass.GoshujinClass, DeferedTestClass>();

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryObjects_AddToGoshujin()
    {
        var objects = default(TemporaryObjects<DeferedTestClass.GoshujinClass, DeferedTestClass>);
        objects.Add(new(1, "a"));
        objects.Add(new(2, "b"));
        objects.Add(new(10, "c"));
        objects.Add(new(100, "d"));

        objects.AddToGoshujin(this.goshujin);
        objects.RemoveFromGoshujin();

        return this.goshujin;
    }
}
