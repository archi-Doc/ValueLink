// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using BenchmarkDotNet.Attributes;
using ValueLink;

namespace Benchmark;

/// <summary>
/// Measures allocations in copying, notifications, owner clearing, and writer reads.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AllocationBenchmark
{
    private readonly AllocationItem.GoshujinClass owner = new();
    private readonly AllocationItem item = new();
    private readonly AllocationItem[] destination = new AllocationItem[1];
    private readonly AllocationRecord.GoshujinClass records = new();
    private AllocationRecord.WriterClass writer = null!;
    private ICollection ordered = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.item.Goshujin = this.owner;
        this.owner.ValueChain.Add(this.item.Value, this.item);
        this.item.PropertyChanged += static (_, _) => { };
        this.ordered = this.owner.ValueChain;
        this.writer = this.records.TryLock(1, AcquisitionMode.CreateOnly)!;
        this.writer.Commit();
    }

    [GlobalCleanup]
    public void Cleanup() => this.writer.Dispose();

    [Benchmark]
    public void CopyOrdered() => this.ordered.CopyTo(this.destination, 0);

    [Benchmark]
    public void NotifySetter() => this.item.ValueValue++;

    [Benchmark]
    public void ClearAndRefill()
    {
        this.owner.ClearAll();
        this.owner.Add(this.item);
    }

    [Benchmark]
    public AllocationRecord? ReadAndCommit()
    {
        _ = this.writer.Id;
        return this.writer.Commit();
    }
}

/// <summary>
/// Provides list ownership and an optional ordered index for allocation measurements.
/// </summary>
[ValueLinkObject]
public partial class AllocationItem
{
    [Link(Type = ChainType.Ordered, GenerateValue = true, AutoNotify = true, AutoLink = false)]
    public int Value { get; set; }

    [Link(Type = ChainType.List, Name = "Items", Primary = true)]
    public AllocationItem()
    {
    }
}

/// <summary>
/// Provides a repeatable-read record for measuring reads and no-op commits.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record AllocationRecord
{
    [Link(Type = ChainType.Unordered, Primary = true, Unique = true)]
    public int Id { get; private set; }
}
