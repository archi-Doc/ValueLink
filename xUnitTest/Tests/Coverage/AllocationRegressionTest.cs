// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Tests allocations in warmed-up collection and generated setter operations.
/// </summary>
public class AllocationRegressionTest
{
    [Theory]
    [InlineData(ChainType.Ordered)]
    [InlineData(ChainType.Unordered)]
    [InlineData(ChainType.SlidingList)]
    public void CopyToDoesNotBoxEnumerators(ChainType kind)
    {
        var owner = new ChainItem.GoshujinClass();
        var item = new ChainItem(1) { Goshujin = owner };
        owner.SlidingChain.Resize(4);
        owner.SlidingChain.Add(item);
        ICollection chain = kind switch
        {
            ChainType.Ordered => owner.OrderedChain,
            ChainType.Unordered => owner.HashChain,
            _ => owner.SlidingChain,
        };
        var destination = new ChainItem[1];
        AssertNoAllocation(() => chain.CopyTo(destination, 0));
        Assert.Same(item, destination[0]);
    }

    [Theory]
    [InlineData(ChainType.Ordered)]
    [InlineData(ChainType.Unordered)]
    [InlineData(ChainType.SlidingList)]
    public void CopyToChecksCovariantDestinationElements(ChainType kind)
    {
        var owner = new ChainItem.GoshujinClass();
        var derived = new DerivedChainItem(1) { Goshujin = owner };
        owner.SlidingChain.Resize(4);
        owner.SlidingChain.Add(derived);
        ICollection chain = kind switch
        {
            ChainType.Ordered => owner.OrderedChain,
            ChainType.Unordered => owner.HashChain,
            _ => owner.SlidingChain,
        };
        var destination = new DerivedChainItem[2];
        chain.CopyTo(destination, 1);
        Assert.Same(derived, destination[1]);
        var other = new ChainItem(2) { Goshujin = owner };
        owner.SlidingChain.Add(other);
        Assert.Throws<ArgumentException>(() => chain.CopyTo(destination, 0));
        Assert.Equal(2, chain.Count);
    }

    [Fact]
    public void GeneratedNotificationsReuseArgumentsAcrossInstances()
    {
        var first = new NotificationItem();
        var second = new NotificationItem();
        PropertyChangedEventArgs? arguments = null;
        first.PropertyChanged += (_, e) => arguments = e;
        first.Value = 1;
        var expected = arguments;
        second.PropertyChanged += (_, e) => arguments = e;
        second.Value = 1;
        Assert.NotNull(expected);
        Assert.Equal(nameof(NotificationItem.Value), expected.PropertyName);
        Assert.Same(expected, arguments);
        AssertNoAllocation(() => first.Value++);
    }

    [Fact]
    public void FloatingPointNaNDoesNotTriggerRedundantUpdates()
    {
        var item = new NotificationItem();
        var notifications = 0;
        item.PropertyChanged += (_, _) => notifications++;
        item.Floating = double.NaN;
        item.Floating = double.NaN;
        item.Single = float.NaN;
        item.Single = float.NaN;
        Assert.Equal(2, notifications);
    }

    private static void AssertNoAllocation(Action action)
    {
        for (var i = 0; i < 1_000; i++)
        {
            action();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++)
        {
            action();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private sealed class DerivedChainItem(int id) : ChainItem(id);

    [Fact]
    public void ClearAllReusesItsSnapshotBuffer()
    {
        var owner = new ClearAllItem.GoshujinClass();
        var item = new ClearAllItem();
        AssertNoAllocation(() =>
        {
            owner.Add(item);
            owner.ClearAll();
        });
        Assert.Null(item.Goshujin);
        Assert.Empty(owner);
    }

    [Fact]
    public void WriterReadsAndUnchangedAssignmentsDoNotCloneRecords()
    {
        var owner = new TrackedEntry.GoshujinClass();
        using var writer = owner.TryLock(1, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        var original = writer.Commit();
        AssertNoAllocation(() =>
        {
            _ = writer.Value;
            writer.Id = 1;
            writer.Value = 0;
            writer.Commit();
        });
        Assert.Same(original, owner.TryGet(1));
    }

    [Fact]
    public void ClearAllPreservesObjectsAddedByRemovalCallbacks()
    {
        var owner = new ClearAllItem.GoshujinClass();
        var added = new ClearAllItem();
        var first = new ClearAllItem { Goshujin = owner, Removed = _ => owner.Add(added) };
        var second = new ClearAllItem { Goshujin = owner };
        owner.ClearAll();
        Assert.Null(first.Goshujin);
        Assert.Null(second.Goshujin);
        Assert.Same(added, Assert.Single(owner));
        Assert.Same(owner, added.Goshujin);
    }
}

/// <summary>
/// Exercises generated notifications for partial properties and floating-point values.
/// </summary>
[ValueLinkObject]
public partial class NotificationItem
{
    [Link(AddValue = true, AutoNotify = true)]
    public partial int Value { get; set; }

    [Link(AddValue = true, AutoNotify = true)]
    public partial double Floating { get; set; }

    [Link(AddValue = true, AutoNotify = true)]
    public partial float Single { get; set; }
}

/// <summary>
/// Exercises pooled snapshots and removal callbacks without allocating chain nodes.
/// </summary>
[ValueLinkObject]
public partial class ClearAllItem
{
    public Action<ClearAllItem>? Removed { get; set; }

    [Link(Type = ChainType.List, Name = "Items", Primary = true)]
    public ClearAllItem()
    {
    }

    private void ItemsLinkRemoved() => this.Removed?.Invoke(this);
}
