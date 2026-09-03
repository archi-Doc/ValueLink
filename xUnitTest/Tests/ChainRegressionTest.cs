// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ValueLink;
using ValueLink.Integrality;
using Xunit;

namespace xUnitTest;

#pragma warning disable xUnit2017 // These tests exercise the chain's Contains implementation.

/// <summary>
/// Tests chain membership and identity regressions.
/// </summary>
public class ChainRegressionTest
{
    [Fact]
    public void ListSearchRejectsForeignAndNullObjects()
    {
        var owner = new ListChainTestClass.GoshujinClass();
        var other = new ListChainTestClass.GoshujinClass();
        var item = new ListChainTestClass(1) { Goshujin = other };
        Assert.False(owner.ListChain.Contains(item));
        Assert.Equal(-1, owner.ListChain.IndexOf(item));
        Assert.False(owner.ListChain.Contains(null!));
        item.Goshujin = owner;
        Assert.True(owner.ListChain.Contains(item));
        Assert.False(other.ListChain.Contains(item));
    }

    [Fact]
    public void ObservableInsertAtEndCanMoveAnExistingObject()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var first = new ObservableChainTestClass(1) { Goshujin = owner };
        var second = new ObservableChainTestClass(2) { Goshujin = owner };
        owner.ObservableChain.Insert(owner.ObservableChain.Count, first);
        Assert.Equal(new[] { second, first }, owner.ObservableChain.ToArray());
        Assert.True(first.ObservableLink.IsLinked);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ObservableInvalidInsertDoesNotRemoveTheObject(int index)
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var first = new ObservableChainTestClass(1) { Goshujin = owner };
        var second = new ObservableChainTestClass(2) { Goshujin = owner };
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.ObservableChain.Insert(index, first));
        Assert.Equal(new[] { first, second }, owner.ObservableChain.ToArray());
        Assert.True(first.ObservableLink.IsLinked);
    }

    [Fact]
    public void ObservableMutationsUseObjectIdentity()
    {
        var owner = new EqualObservableItem.GoshujinClass();
        var first = new EqualObservableItem { Goshujin = owner };
        var second = new EqualObservableItem { Goshujin = owner };
        Assert.Equal(1, owner.ItemsChain.IndexOf(second));
        Assert.True(owner.ItemsChain.Remove(second));
        Assert.Same(first, owner.ItemsChain[0]);
        Assert.True(first.ItemsLink.IsLinked);
        Assert.False(second.ItemsLink.IsLinked);
        Assert.False(owner.ItemsChain.Contains(second));
        owner.ItemsChain.Add(second);
        owner.ItemsChain.Add(first);
        Assert.Same(second, owner.ItemsChain[0]);
        Assert.Same(first, owner.ItemsChain[1]);
        owner.ItemsChain[0] = first;
        Assert.Single(owner.ItemsChain);
        Assert.Same(first, owner.ItemsChain[0]);
        Assert.False(second.ItemsLink.IsLinked);
    }

    [Fact]
    public void ObservableNotificationsSeeConsistentLinks()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var first = new ObservableChainTestClass(1) { Goshujin = owner };
        var second = new ObservableChainTestClass(2) { Goshujin = owner };
        var chain = owner.ObservableChain;
        chain.Clear();
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, e) =>
        {
            foreach (var item in new[] { first, second })
            {
                Assert.Equal(chain.Any(x => ReferenceEquals(x, item)), item.ObservableLink.IsLinked);
            }
        };
        chain.Add(first);
        chain.Insert(0, second);
        chain[0] = first;
        chain.Remove(first);
        chain.Add(second);
        chain.Clear();
    }

    [Fact]
    public void ObservableThrowingSubscriberDoesNotLeaveStaleLinks()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var item = new ObservableChainTestClass(1) { Goshujin = owner };
        var chain = owner.ObservableChain;
        chain.Clear();
        NotifyCollectionChangedEventHandler handler = (_, _) => throw new InvalidOperationException("Subscriber failed");
        ((INotifyCollectionChanged)chain).CollectionChanged += handler;
        Assert.Throws<InvalidOperationException>(() => chain.Add(item));
        Assert.True(item.ObservableLink.IsLinked);
        Assert.Same(item, chain[0]);
        Assert.Throws<InvalidOperationException>(() => chain.Remove(item));
        Assert.False(item.ObservableLink.IsLinked);
        Assert.Empty(chain);
        ((INotifyCollectionChanged)chain).CollectionChanged -= handler;
        chain.Add(item);
        Assert.Single(chain);
    }

    [Fact]
    public void ObservableRejectedReentrancyPreservesLinks()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var item = new ObservableChainTestClass(1) { Goshujin = owner };
        var chain = owner.ObservableChain;
        chain.Clear();
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, _) => Assert.Throws<InvalidOperationException>(() => chain.Clear());
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, _) => Assert.True(item.ObservableLink.IsLinked);
        chain.Add(item);
        Assert.Single(chain);
        Assert.True(item.ObservableLink.IsLinked);
    }

    [Fact]
    public void ObservableInvalidMoveDoesNotRemoveTheObject()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var item = new ObservableChainTestClass(1) { Goshujin = owner };
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.ObservableChain.Move(0, 1));
        Assert.Same(item, owner.ObservableChain[0]);
        Assert.True(item.ObservableLink.IsLinked);
    }

    [Fact]
    public void ObservableRefOverloadsUpdateTheSuppliedLinkBeforeNotification()
    {
        var owner = new ObservableChainTestClass.GoshujinClass();
        var item = new ObservableChainTestClass(1) { Goshujin = owner };
        var chain = owner.ObservableChain;
        chain.Clear();
        ObservableChain<ObservableChainTestClass>.Link link = default;
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, _) => Assert.Equal(chain.Count != 0, link.IsLinked);
        chain.Add(item, ref link);
        chain.Add(item, ref link);
        Assert.Single(chain);
        Assert.False(item.ObservableLink.IsLinked);
        Assert.True(chain.Remove(item, ref link));
        Assert.False(chain.Remove(item, ref link));
        Assert.Empty(chain);
    }

    [Fact]
    public void OrderedClearUnlinksDuplicateKeysAndSupportsReuse()
    {
        var owner = new ClearTestClass.GoshujinClass();
        var items = Enumerable.Range(0, 100).Select(x => new ClearTestClass(x % 5) { Goshujin = owner }).ToArray();
        owner.IdChain.Clear();
        Assert.Empty(owner.IdChain);
        foreach (var item in items)
        {
            Assert.False(item.IdLink.IsLinked);
            owner.IdChain.Add(item.Id, item);
        }

        Assert.Equal(items.Length, owner.IdChain.Count);
        Assert.Equal(items.OrderBy(x => x.Id), owner.IdChain);
    }

    [Fact]
    public void SlidingCountExcludesHolesAndClearPreservesPositions()
    {
        var owner = new SlidingListChainClass.GoshujinClass();
        var chain = owner.SlidingListChain;
        chain.Resize(4);
        var items = Enumerable.Range(0, 4).Select(x => new SlidingListChainClass(x) { Goshujin = owner }).ToArray();
        foreach (var item in items)
        {
            Assert.True(chain.Add(item));
        }

        chain.Remove(items[1]);
        Assert.Equal(3, chain.Count);
        Assert.Equal(3, ((System.Collections.ICollection)chain).Count);
        Assert.Equal(3, ((IReadOnlyCollection<SlidingListChainClass>)chain).Count);
        Assert.Equal(4, chain.Consumed);
        Assert.Equal(3, chain.ToArray().Length);
        var end = chain.EndPosition;
        chain.Clear();
        Assert.Empty(chain);
        Assert.Equal(end, chain.StartPosition);
        foreach (var item in items)
        {
            Assert.False(item.SlidingListLink.IsLinked);
            Assert.True(chain.Add(item));
        }

        Assert.Equal(4, chain.Count);
    }

    [Fact]
    public void LargeIntegralityHashIgnoresUnusedPooledBytes()
    {
        var owner = new SimpleIntegralityClass.GoshujinClass();
        const int count = 400;
        for (var i = 0; i < count; i++)
        {
            owner.Add(new SimpleIntegralityClass(i, "Item"));
        }

        var hashObject = (IIntegralityObject)owner;
        static void FillPool(byte value)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(count * (sizeof(int) + sizeof(ulong)));
            Array.Fill(buffer, value);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        FillPool(0x11);
        var first = hashObject.GetIntegralityHash();
        hashObject.ClearIntegralityHash();
        FillPool(0xEE);
        Assert.Equal(first, hashObject.GetIntegralityHash());
    }
}

/// <summary>
/// Provides equal-valued reference objects for identity-based collection tests.
/// </summary>
[ValueLinkObject]
public partial class EqualObservableItem
{
    [Link(Type = ChainType.Observable, Name = "Items", Primary = true)]
    public int Id { get; set; }

    public override bool Equals(object? obj) => obj is EqualObservableItem;

    public override int GetHashCode() => 0;
}
