// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
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
    private readonly DeferedTestClass.GoshujinClass goshujin = new();

    public DeferedListBenchmark()
    {
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass List_AddAndRemove()
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
    public DeferedTestClass.GoshujinClass List_AddAndRemove2()
    {
        var list = new DeferredList<DeferedTestClass.GoshujinClass, DeferedTestClass>(this.goshujin);
        list.Add(new(1, "a"));
        list.Add(new(2, "b"));
        list.Add(new(10, "c"));
        list.Add(new(100, "d"));

        foreach (var x in list)
        {
            goshujin.Add(x);
        }

        list.Clear();

        foreach (var x in this.goshujin)
        {
            list.Add(x);
        }

        foreach (var x in list)
        {
            goshujin.Remove(x);
        }

        list.Clear();

        return goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryQueue_AddAndRemove()
    {
        var queue = default(TemporaryQueue<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        foreach (var x in queue)
        {
            this.goshujin.Add(x);
        }

        foreach (var x in queue)
        {
            this.goshujin.Remove(x);
        }

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryQueue_AddAndRemove2()
    {
        var queue = default(TemporaryQueue<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        this.goshujin.AddAll(ref queue);
        this.goshujin.RemoveAll(ref queue);

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryObject_AddAndRemove()
    {
        var queue = default(TemporaryObjects<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        queue.AddToGoshujin(this.goshujin);
        queue.RemoveFromGoshunin(this.goshujin);

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass Queue_AddAndRemove()
    {
        var d = new DeferredQueue<DeferedTestClass.GoshujinClass, DeferedTestClass>(this.goshujin);
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
}
