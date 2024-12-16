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

    // [Benchmark]
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

    // [Benchmark]
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
    public DeferedTestClass.GoshujinClass TemporaryQueue_ForEach()
    {
        var queue = default(TemporaryQueue<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        foreach (var x in queue)
        {
            x.Goshujin = this.goshujin;
            // this.goshujin.Add(x);
        }

        foreach (var x in queue)
        {
            x.Goshujin = default;
            // this.goshujin.Remove(x);
        }

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryQueue_GoshujinAddAll()
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
    public DeferedTestClass.GoshujinClass TemporaryQueue_QueueAddToGoshujin()
    {
        var queue = default(TemporaryQueue<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        queue.AddToGoshujin(this.goshujin);
        queue.RemoveFromGoshujin<DeferedTestClass.GoshujinClass, DeferedTestClass>();

        return this.goshujin;
    }

    [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryQueue_ObjectsAddToGoshujin()
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

    // [Benchmark]
    public DeferedTestClass.GoshujinClass TemporaryQueue_AddAndRemove5()
    {
        var queue = default(TemporaryQueue<DeferedTestClass>);
        queue.Enqueue(new(1, "a"));
        queue.Enqueue(new(2, "b"));
        queue.Enqueue(new(10, "c"));
        queue.Enqueue(new(100, "d"));

        var array = new DeferedTestClass[queue.Count];
        var e = queue.GetEnumerator();
        for (var i = 0; i < array.Length; i++)
        {
            e.MoveNext();
            array[i] = e.Current;
        }

        var objects = array.AsSpan();
        ValueLinkHelper.SetGoshujin2<DeferedTestClass.GoshujinClass, DeferedTestClass>(objects, this.goshujin);
        ValueLinkHelper.SetGoshujin2<DeferedTestClass.GoshujinClass, DeferedTestClass>(objects, default);

        return this.goshujin;
    }

    // [Benchmark]
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

    // [Benchmark]
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
