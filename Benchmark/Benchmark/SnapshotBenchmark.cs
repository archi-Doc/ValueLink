// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using ValueLink;

namespace Benchmark;

/// <summary>
/// Provides a Serializable owner with a hash primary chain for snapshot measurements.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SnapshotItem
{
    [Link(Type = ChainType.Unordered, Primary = true)]
    public int Id { get; set; }
}

/// <summary>
/// Provides a Serializable owner with a list primary chain for snapshot measurements.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SnapshotListItem
{
    [Link(Type = ChainType.List, Name = "Items", Primary = true)]
    public SnapshotListItem()
    {
    }
}

/// <summary>
/// Measures owner snapshots taken under the owner lock.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SnapshotBenchmark
{
    private readonly SnapshotItem.GoshujinClass hashOwner = new();
    private readonly SnapshotListItem.GoshujinClass listOwner = new();
    private readonly IsolationClass.GoshujinClass orderedOwner = new();

    [Params(16, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < this.Count; i++)
        {
            this.hashOwner.Add(new() { Id = i });
            this.listOwner.Add(new());
            using (var w = this.orderedOwner.TryLock(i, AcquisitionMode.CreateOnly))
            {
                w?.Commit();
            }
        }
    }

    [Benchmark]
    public SnapshotItem[] SerializableUnordered() => this.hashOwner.GetArray();

    [Benchmark]
    public SnapshotListItem[] SerializableList() => this.listOwner.GetArray();

    [Benchmark]
    public IsolationClass[] RepeatableReadOrdered() => this.orderedOwner.GetArray();
}
