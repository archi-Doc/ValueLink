// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

/// <summary>
/// Provides a fixture for tests of observable replacement and movement.
/// </summary>
[ValueLinkObject]
public partial class ObservableChainTestClass
{
    [Link(Primary = true, Type = ChainType.Observable, Name = "Observable")]
    public int Id { get; set; }

    public ObservableChainTestClass(int id)
    {
        this.Id = id;
    }
}

/// <summary>
/// Tests observable replacement and movement.
/// </summary>
public class ObservableChainTest
{
    private static ObservableChainTestClass.GoshujinClass CreateGoshujin(int count)
    {
        var g = new ObservableChainTestClass.GoshujinClass();
        for (var i = 0; i < count; i++)
        {
            new ObservableChainTestClass(i).Goshujin = g;
        }

        return g;
    }

    [Fact]
    public void Set_ReplacesTheElementInPlace()
    {
        var g = CreateGoshujin(4);
        var chain = g.ObservableChain;
        var replaced = chain[2];

        var fresh = new ObservableChainTestClass(99) { Goshujin = g, };
        chain.Remove(fresh);
        chain.Count.Is(4);

        NotifyCollectionChangedAction? action = null;
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, e) => action = e.Action;

        chain[2] = fresh;

        chain.Count.Is(4); // Replace, not insert.
        chain[2].Is(fresh);
        action.Is(NotifyCollectionChangedAction.Replace);
        replaced.ObservableLink.IsLinked.IsFalse();
        fresh.ObservableLink.IsLinked.IsTrue();
        chain.Contains(replaced).IsFalse();
    }

    [Fact]
    public void Set_WithTheSameObject_IsANoOp()
    {
        var g = CreateGoshujin(3);
        var chain = g.ObservableChain;
        var obj = chain[1];

        var raised = false;
        ((INotifyCollectionChanged)chain).CollectionChanged += (_, _) => raised = true;

        chain[1] = obj;

        chain.Count.Is(3);
        chain[1].Is(obj);
        raised.IsFalse();
    }

    [Fact]
    public void Set_WithAnObjectAlreadyInTheChain_MovesItAndShrinks()
    {
        var g = CreateGoshujin(5);
        var chain = g.ObservableChain;
        var moved = chain[0];
        var replaced = chain[3];

        chain[3] = moved;

        chain.Count.Is(4); // Two slots collapsed into one.
        chain.Select(x => x.Id).SequenceEqual([1, 2, 0, 4,]).IsTrue();
        moved.ObservableLink.IsLinked.IsTrue();
        replaced.ObservableLink.IsLinked.IsFalse();
        chain.Contains(replaced).IsFalse();
    }

    [Fact]
    public void Set_RejectsInvalidArguments()
    {
        var g = CreateGoshujin(2);
        var chain = g.ObservableChain;
        var fresh = new ObservableChainTestClass(99) { Goshujin = g, };
        chain.Remove(fresh);

        Assert.Throws<ArgumentOutOfRangeException>(() => chain[2] = fresh);
        Assert.Throws<ArgumentOutOfRangeException>(() => chain[-1] = fresh);

        var other = new ObservableChainTestClass(1) { Goshujin = new ObservableChainTestClass.GoshujinClass(), };
        Assert.Throws<UnmatchedGoshujinException>(() => chain[0] = other);
        chain.Count.Is(2);
    }
}
