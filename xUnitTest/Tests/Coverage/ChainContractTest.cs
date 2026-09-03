// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Provides a fixture for tests of the shared contracts of all chain implementations.
/// </summary>
[ValueLinkObject]
public partial class ChainItem
{
    [Link(Type = ChainType.Ordered, Name = "Ordered", AddValue = true)]
    [Link(Type = ChainType.ReverseOrdered, Name = "Reverse")]
    [Link(Type = ChainType.Unordered, Name = "Hash")]
    public int Id { get; set; }

    [Link(Type = ChainType.List, Name = "List", Primary = true)]
    [Link(Type = ChainType.LinkedList, Name = "Linked")]
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    [Link(Type = ChainType.StackList, Name = "Stack")]
    [Link(Type = ChainType.Observable, Name = "Observable")]
    [Link(Type = ChainType.SlidingList, Name = "Sliding")]
    public ChainItem(int id) => this.Id = id;
}

/// <summary>
/// Tests the shared contracts of all chain implementations.
/// </summary>
public class ChainContractTest
{
    public static IEnumerable<object[]> Chains => Enum.GetValues<ChainType>()
        .Where(x => x != ChainType.None).Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(Chains))]
    public void AddRemoveClearAndReuseMaintainMembership(ChainType kind)
    {
        var owner = NewOwner();
        var items = Enumerable.Range(0, 12).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        Clear(owner, kind);
        Assert.Empty(Chain(owner, kind));
        foreach (var item in items)
        {
            Assert.False(IsLinked(item, kind));
            Add(owner, kind, item, false);
            Assert.True(IsLinked(item, kind));
        }

        Assert.Equal(items, Chain(owner, kind).OrderBy(x => x.Id));
        Assert.True(Remove(owner, kind, items[3], false));
        Assert.False(Remove(owner, kind, items[3], true));
        Assert.False(IsLinked(items[3], kind));
        Assert.Same(owner, items[3].Goshujin);
        Add(owner, kind, items[3], true);
        Assert.Equal(items, Chain(owner, kind).OrderBy(x => x.Id));
        foreach (var item in items)
        {
            Assert.True(Remove(owner, kind, item, true));
        }

        Assert.Empty(Chain(owner, kind));
        foreach (var item in items)
        {
            Add(owner, kind, item, false);
        }

        Clear(owner, kind);
        Clear(owner, kind);
        Assert.All(items, item => Assert.False(IsLinked(item, kind)));
    }

    [Theory]
    [MemberData(nameof(Chains))]
    public void ForeignOwnerMutationThrowsWithoutChangingEitherChain(ChainType kind)
    {
        var owner = NewOwner();
        var other = NewOwner();
        var item = new ChainItem(1) { Goshujin = other };
        if (kind == ChainType.SlidingList)
        {
            Add(other, kind, item, false);
        }

        Assert.Throws<UnmatchedGoshujinException>(() => Add(owner, kind, item, false));
        Assert.Throws<UnmatchedGoshujinException>(() => Add(owner, kind, item, true));
        Assert.Throws<UnmatchedGoshujinException>(() => Remove(owner, kind, item, false));
        Assert.Throws<UnmatchedGoshujinException>(() => Remove(owner, kind, item, true));
        Assert.Empty(Chain(owner, kind));
        Assert.Same(item, Assert.Single(Chain(other, kind)));
        Assert.True(IsLinked(item, kind));
    }

    [Theory]
    [MemberData(nameof(Chains))]
    public void EnumerationAndCopyExposeExactlyTheLiveObjects(ChainType kind)
    {
        var owner = NewOwner();
        var items = Enumerable.Range(0, 5).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        if (kind == ChainType.SlidingList)
        {
            foreach (var item in items)
            {
                Add(owner, kind, item, false);
            }
        }

        Remove(owner, kind, items[2], false);
        var enumerable = Chain(owner, kind);
        var expected = enumerable.ToArray();
        Assert.Equal(expected, ((IEnumerable)enumerable).Cast<ChainItem>());
        var destination = new ChainItem[expected.Length + 2];
        if (kind == ChainType.List)
        {
            owner.ListChain.CopyTo(destination, 1);
        }
        else
        {
            var collection = (ICollection)enumerable;
            Assert.Equal(expected.Length, collection.Count);
            Assert.False(collection.IsSynchronized);
            Assert.Same(enumerable, collection.SyncRoot);
            collection.CopyTo(destination, 1);
        }

        Assert.Null(destination[0]);
        Assert.Null(destination[^1]);
        Assert.Equal(expected, destination.Skip(1).Take(expected.Length));
    }

    [Theory]
    [InlineData(ChainType.Ordered)]
    [InlineData(ChainType.ReverseOrdered)]
    [InlineData(ChainType.Unordered)]
    [InlineData(ChainType.SlidingList)]
    public void CopyValidatesDestinationAndSupportsObjectArrays(ChainType kind)
    {
        var owner = NewOwner();
        var item = new ChainItem(7) { Goshujin = owner };
        owner.SlidingChain.Add(item);
        var collection = (ICollection)Chain(owner, kind);
        var destination = new object[3];
        collection.CopyTo(destination, 1);
        Assert.Equal(new object?[] { null, item, null }, destination);
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(destination, -1));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(destination, int.MaxValue));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(destination, 3));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new object[1, 1], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(Array.CreateInstance(typeof(object), new[] { 2 }, new[] { 1 }), 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new int[1], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new string[1], 0));
        Assert.Same(item, Assert.Single(Chain(owner, kind)));
        Clear(owner, kind);
        collection.CopyTo(Array.Empty<ChainItem>(), 0);
    }

    [Fact]
    public void OwnerTransferAndKeyUpdateKeepAllIndexesConsistent()
    {
        var owner = NewOwner();
        var other = NewOwner();
        var item = new ChainItem(4) { Goshujin = owner };
        Assert.True(owner.SlidingChain.Add(item));
        item.OrderedValue = 9;
        Assert.Null(owner.OrderedChain.FindFirst(4));
        Assert.Null(owner.HashChain.FindFirst(4));
        Assert.Same(item, owner.OrderedChain[9]);
        Assert.Same(item, owner.ReverseChain[9]);
        Assert.Same(item, owner.HashChain[9]);

        item.Goshujin = other;
        foreach (var kind in Enum.GetValues<ChainType>().Where(x => x != ChainType.None))
        {
            Assert.Empty(Chain(owner, kind));
            if (kind != ChainType.SlidingList)
            {
                Assert.Same(item, Assert.Single(Chain(other, kind)));
            }
        }

        Assert.False(item.SlidingLink.IsLinked);
        other.ClearAll();
        Assert.Null(item.Goshujin);
        Assert.All(Enum.GetValues<ChainType>().Where(x => x != ChainType.None), kind => Assert.Empty(Chain(other, kind)));
    }

    internal static ChainItem.GoshujinClass NewOwner()
    {
        var owner = new ChainItem.GoshujinClass();
        owner.SlidingChain.Resize(128);
        return owner;
    }

    private static IEnumerable<ChainItem> Chain(ChainItem.GoshujinClass g, ChainType kind) => kind switch
    {
        ChainType.Ordered => g.OrderedChain,
        ChainType.ReverseOrdered => g.ReverseChain,
        ChainType.Unordered => g.HashChain,
        ChainType.List => g.ListChain,
        ChainType.LinkedList => g.LinkedChain,
        ChainType.QueueList => g.QueueChain,
        ChainType.StackList => g.StackChain,
        ChainType.Observable => g.ObservableChain,
        ChainType.SlidingList => g.SlidingChain,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static bool IsLinked(ChainItem x, ChainType kind) => kind switch
    {
        ChainType.Ordered => x.OrderedLink.IsLinked,
        ChainType.ReverseOrdered => x.ReverseLink.IsLinked,
        ChainType.Unordered => x.HashLink.IsLinked,
        ChainType.List => x.ListLink.IsLinked,
        ChainType.LinkedList => x.LinkedLink.IsLinked,
        ChainType.QueueList => x.QueueLink.IsLinked,
        ChainType.StackList => x.StackLink.IsLinked,
        ChainType.Observable => x.ObservableLink.IsLinked,
        ChainType.SlidingList => x.SlidingLink.IsLinked,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static void Clear(ChainItem.GoshujinClass g, ChainType kind)
    {
        switch (kind)
        {
            case ChainType.Ordered: g.OrderedChain.Clear(); break;
            case ChainType.ReverseOrdered: g.ReverseChain.Clear(); break;
            case ChainType.Unordered: g.HashChain.Clear(); break;
            case ChainType.List: g.ListChain.Clear(); break;
            case ChainType.LinkedList: g.LinkedChain.Clear(); break;
            case ChainType.QueueList: g.QueueChain.Clear(); break;
            case ChainType.StackList: g.StackChain.Clear(); break;
            case ChainType.Observable: g.ObservableChain.Clear(); break;
            case ChainType.SlidingList: g.SlidingChain.Clear(); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void Add(ChainItem.GoshujinClass g, ChainType kind, ChainItem x, bool byRef)
    {
        switch (kind)
        {
            case ChainType.Ordered: if (byRef) g.OrderedChain.Add(x.Id, x, ref x.OrderedLink); else g.OrderedChain.Add(x.Id, x); break;
            case ChainType.ReverseOrdered: if (byRef) g.ReverseChain.Add(x.Id, x, ref x.ReverseLink); else g.ReverseChain.Add(x.Id, x); break;
            case ChainType.Unordered: if (byRef) g.HashChain.Add(x.Id, x, ref x.HashLink); else g.HashChain.Add(x.Id, x); break;
            case ChainType.List: if (byRef) g.ListChain.Add(x, ref x.ListLink); else g.ListChain.Add(x); break;
            case ChainType.LinkedList: if (byRef) g.LinkedChain.AddLast(x, ref x.LinkedLink); else g.LinkedChain.AddLast(x); break;
            case ChainType.QueueList: if (byRef) g.QueueChain.Enqueue(x, ref x.QueueLink); else g.QueueChain.Enqueue(x); break;
            case ChainType.StackList: if (byRef) g.StackChain.Push(x, ref x.StackLink); else g.StackChain.Push(x); break;
            case ChainType.Observable: if (byRef) g.ObservableChain.Add(x, ref x.ObservableLink); else g.ObservableChain.Add(x); break;
            case ChainType.SlidingList: if (byRef) g.SlidingChain.Add(x, ref x.SlidingLink); else g.SlidingChain.Add(x); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static bool Remove(ChainItem.GoshujinClass g, ChainType kind, ChainItem x, bool byRef) => kind switch
    {
        ChainType.Ordered => byRef ? g.OrderedChain.Remove(x, ref x.OrderedLink) : g.OrderedChain.Remove(x),
        ChainType.ReverseOrdered => byRef ? g.ReverseChain.Remove(x, ref x.ReverseLink) : g.ReverseChain.Remove(x),
        ChainType.Unordered => byRef ? g.HashChain.Remove(x, ref x.HashLink) : g.HashChain.Remove(x),
        ChainType.List => byRef ? g.ListChain.Remove(x, ref x.ListLink) : g.ListChain.Remove(x),
        ChainType.LinkedList => byRef ? g.LinkedChain.Remove(x, ref x.LinkedLink) : g.LinkedChain.Remove(x),
        ChainType.QueueList => byRef ? g.QueueChain.Remove(x, ref x.QueueLink) : g.QueueChain.Remove(x),
        ChainType.StackList => byRef ? g.StackChain.Remove(x, ref x.StackLink) : g.StackChain.Remove(x),
        ChainType.Observable => byRef ? g.ObservableChain.Remove(x, ref x.ObservableLink) : g.ObservableChain.Remove(x),
        ChainType.SlidingList => byRef ? g.SlidingChain.Remove(x, ref x.SlidingLink) : g.SlidingChain.Remove(x),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
