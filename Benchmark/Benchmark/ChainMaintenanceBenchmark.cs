// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using ValueLink;

namespace Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
public class ChainMaintenanceBenchmark
{
    private MaintenanceItem.GoshujinClass owner = new();
    private MaintenanceItem[] items = [];

    [Params(32, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.owner = new();
        this.owner.SlidingChain.Resize(this.Count);
        this.items = new MaintenanceItem[this.Count];
        for (var i = 0; i < this.Count; i++)
        {
            this.items[i] = new MaintenanceItem { Id = i, Goshujin = this.owner };
        }
    }

    [Benchmark]
    public void OrderedClearAndRefill()
    {
        this.owner.IdChain.Clear();
        foreach (var item in this.items)
        {
            this.owner.IdChain.Add(item.Id, item);
        }
    }

    [Benchmark]
    public void SlidingClearAndRefill()
    {
        this.owner.SlidingChain.Clear();
        foreach (var item in this.items)
        {
            this.owner.SlidingChain.Add(item);
        }
    }

    [Benchmark]
    public bool ObservableContains() => this.owner.ObservableChain.Contains(this.items[^1]);
}

[ValueLinkObject]
public partial class MaintenanceItem
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [Link(Type = ChainType.SlidingList, Name = "Sliding")]
    [Link(Type = ChainType.Observable, Name = "Observable")]
    public int Id { get; set; }
}
