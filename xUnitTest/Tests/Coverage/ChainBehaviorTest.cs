// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Tests collection ordering, bounds, notifications, and randomized mutations.
/// </summary>
public class ChainBehaviorTest
{
    [Fact]
    public void QueueAndStackPreserveTheirOrderingWhenRelinking()
    {
        var owner = ChainContractTest.NewOwner();
        var items = Enumerable.Range(0, 4).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        var queue = owner.QueueChain;
        var stack = owner.StackChain;
        queue.TryEnqueue(items[0]);
        stack.TryPush(items[0]);
        Assert.Same(items[0], queue.Peek());
        Assert.Same(items[3], stack.Peek());
        queue.Enqueue(items[0], ref items[0].QueueLink);
        stack.Push(items[0], ref items[0].StackLink);
        foreach (var index in new[] { 1, 2, 3, 0 })
        {
            Assert.True(queue.TryPeek(out var next));
            Assert.Same(items[index], next);
            Assert.Same(next, queue.Dequeue());
            Assert.False(next.QueueLink.IsLinked);
        }

        foreach (var index in new[] { 0, 3, 2, 1 })
        {
            Assert.True(stack.TryPeek(out var next));
            Assert.Same(items[index], next);
            Assert.True(stack.TryPop(out var removed));
            Assert.Same(next, removed);
            Assert.False(removed.StackLink.IsLinked);
        }

        queue.TryEnqueue(items[0]);
        Assert.True(queue.TryDequeue(out var last));
        Assert.Same(items[0], last);
        stack.TryPush(items[0]);
        Assert.Same(items[0], stack.Pop());
        Assert.False(queue.TryPeek(out var q));
        Assert.Null(q);
        Assert.False(queue.TryDequeue(out q));
        Assert.Null(q);
        Assert.False(stack.TryPeek(out var s));
        Assert.Null(s);
        Assert.False(stack.TryPop(out s));
        Assert.Null(s);
        Assert.Throws<InvalidOperationException>(() => queue.Peek());
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        Assert.Throws<InvalidOperationException>(() => stack.Peek());
        Assert.Throws<InvalidOperationException>(() => stack.Pop());
    }

    [Fact]
    public void LinkedListMovesKeepBothDirectionsConnected()
    {
        var owner = ChainContractTest.NewOwner();
        var items = Enumerable.Range(0, 4).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        var chain = owner.LinkedChain;
        chain.TryAddFirst(items[2]);
        chain.TryAddLast(items[0]);
        Assert.Equal(items, chain);
        chain.AddFirst(items[3]);
        chain.AddLast(items[0], ref items[0].LinkedLink);
        var expected = new[] { items[3], items[1], items[2], items[0] };
        Assert.Equal(expected, chain);
        Assert.Same(expected[0], chain.First);
        Assert.Same(expected[^1], chain.Last);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Same(i == 0 ? null : expected[i - 1], expected[i].LinkedLink.Previous);
            Assert.Same(i == expected.Length - 1 ? null : expected[i + 1], expected[i].LinkedLink.Next);
            Assert.Same(expected[i], chain.Find(expected[i]));
        }

        chain.Remove(items[1]);
        Assert.Null(items[1].LinkedLink.Previous);
        Assert.Null(items[1].LinkedLink.Next);
        Assert.Null(chain.Find(items[1]));
        Assert.Same(items[2], items[3].LinkedLink.Next);
        Assert.Same(items[3], items[2].LinkedLink.Previous);
        chain.Clear();
        chain.TryAddFirst(items[0]);
        chain.TryAddLast(items[1]);
        Assert.Equal(items.Take(2), chain);
    }

    [Fact]
    public void KeyedChainsHandleDuplicatesBoundsAndKeyChanges()
    {
        var owner = ChainContractTest.NewOwner();
        var items = new[] { 5, 1, 3, 3, 9 }.Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        Assert.Equal(new[] { 1, 3, 3, 5, 9 }, owner.OrderedChain.Keys);
        Assert.Equal(new[] { 9, 5, 3, 3, 1 }, owner.ReverseChain.Keys);
        Assert.False(owner.OrderedChain.Reverse);
        Assert.True(owner.ReverseChain.Reverse);
        Assert.Equal(2, owner.OrderedChain.Enumerate(3).Count());
        Assert.Equal(2, owner.HashChain.Enumerate(3).Count());
        Assert.Equal(5, owner.OrderedChain.GetLowerBound(4)!.Id);
        Assert.Equal(3, owner.OrderedChain.GetUpperBound(4)!.Id);
        Assert.Null(owner.OrderedChain.GetLowerBound(10));
        Assert.Null(owner.OrderedChain.GetUpperBound(0));
        var range = owner.OrderedChain.GetRange(2, 6);
        Assert.Equal(3, range.Lower!.Id);
        Assert.Equal(5, range.Upper!.Id);
        Assert.Equal((null, null), owner.OrderedChain.GetRange(6, 8));
        Assert.Equal(owner.OrderedChain, owner.OrderedChain.Objects);
        Assert.Equal(owner.OrderedChain.Keys, owner.OrderedChain.KeyObjects.Select(x => x.Key));
        Assert.Equal(owner.HashChain, owner.HashChain.Objects);
        Assert.Equal(owner.HashChain.Keys, owner.HashChain.KeyObjects.Select(x => x.Key));
        Assert.True(owner.HashChain.TryGetValue(5, out var found));
        Assert.Same(items[0], found);
        Assert.False(owner.OrderedChain.TryGetValue(0, out found));
        Assert.Null(found);

        items[2].OrderedValue = 8;
        Assert.Same(items[3], Assert.Single(owner.OrderedChain.Enumerate(3)));
        Assert.Same(items[2], owner.HashChain.FindFirst(8));
        Assert.Equal(new[] { 1, 3, 5, 8, 9 }, owner.OrderedChain.Keys);
        Assert.Equal(new[] { 9, 8, 5, 3, 1 }, owner.ReverseChain.Keys);
        Assert.Equal(5, owner.HashChain.Count);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(8128)]
    [InlineData(104729)]
    public void RandomListMutationsPreserveMembershipAndIndexes(int seed)
    {
        var random = new Random(seed);
        var owner = ChainContractTest.NewOwner();
        var items = Enumerable.Range(0, 150).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        var chain = owner.ListChain;
        var members = items.ToHashSet();
        for (var step = 0; step < 700; step++)
        {
            var item = items[random.Next(items.Length)];
            switch (random.Next(6))
            {
                case 0:
                    chain.Add(item);
                    members.Add(item);
                    Assert.Same(item, chain[^1]);
                    break;
                case 1:
                    Assert.Equal(members.Remove(item), chain.Remove(item));
                    break;
                case 2:
                    var index = random.Next(chain.Count + 1);
                    chain.Insert(index, item);
                    members.Add(item);
                    Assert.Same(item, chain[Math.Min(index, chain.Count - 1)]);
                    break;
                case 3 when chain.Count > 0:
                    index = random.Next(chain.Count);
                    members.Remove(chain[index]);
                    chain[index] = item;
                    members.Add(item);
                    break;
                case 4 when chain.Count > 0:
                    index = random.Next(chain.Count);
                    members.Remove(chain[index]);
                    chain.RemoveAt(index);
                    break;
                case 5 when step % 37 == 0:
                    chain.Clear();
                    members.Clear();
                    break;
            }

            Assert.True(members.SetEquals(chain), $"Membership mismatch: seed {seed}, step {step}");
            Assert.Equal(members.Count, chain.Count);
            for (var i = 0; i < chain.Count; i++)
            {
                Assert.Equal(i, chain[i].ListLink.Index);
                Assert.Equal(i, chain.IndexOf(chain[i]));
            }

            Assert.All(items, x => Assert.Equal(members.Contains(x), x.ListLink.IsLinked));
        }
    }

    [Fact]
    public void SlidingWindowPreservesPositionsThroughHolesAndResize()
    {
        var owner = new ChainItem.GoshujinClass();
        var items = Enumerable.Range(0, 6).Select(x => new ChainItem(x) { Goshujin = owner }).ToArray();
        var chain = owner.SlidingChain;
        Assert.False(chain.Add(items[0]));
        Assert.False(items[0].SlidingLink.IsLinked);
        Assert.True(chain.Resize(4));
        foreach (var item in items.Take(4))
        {
            Assert.True(chain.Add(item));
        }

        var start = chain.StartPosition;
        Assert.False(chain.CanAdd);
        Assert.False(chain.Add(items[4]));
        Assert.False(chain.Add(items[0]));
        Assert.True(chain.Remove(items[1]));
        Assert.Equal(3, chain.Count);
        Assert.Equal(4, chain.Consumed);
        Assert.False(chain.Resize(2));
        Assert.Equal(4, chain.Capacity);
        Assert.True(chain.Set(start + 1, items[4]));
        Assert.True(chain.Set(start + 2, items[5]));
        Assert.False(items[2].SlidingLink.IsLinked);
        Assert.False(chain.Set(start - 1, items[2]));
        Assert.True(chain.Resize(8));
        Assert.Same(items[4], chain.Get(start + 1));
        Assert.Equal(start + 1, items[4].SlidingLink.Position);
        Assert.Same(items[5], chain.Get(start + 2));
        Assert.True(chain.Remove(items[0]));
        Assert.Equal(start + 1, chain.StartPosition);
        Assert.Same(items[4], chain.FirstOrDefault);
        Assert.True(chain.Add(items[2], ref items[2].SlidingLink));
        Assert.Equal(4, chain.Count);
    }

    [Fact]
    public void ObservableEventsDescribeTheMutationAndCanBeUnsubscribed()
    {
        var owner = ChainContractTest.NewOwner();
        var first = new ChainItem(1) { Goshujin = owner };
        var second = new ChainItem(2) { Goshujin = owner };
        var chain = owner.ObservableChain;
        chain.Clear();
        var actions = new List<NotifyCollectionChangedEventArgs>();
        var properties = new List<string?>();
        NotifyCollectionChangedEventHandler changed = (_, e) => actions.Add(e);
        PropertyChangedEventHandler propertyChanged = (_, e) => properties.Add(e.PropertyName);
        ((INotifyCollectionChanged)chain).CollectionChanged += changed;
        ((INotifyPropertyChanged)chain).PropertyChanged += propertyChanged;
        chain.Add(first);
        chain.Insert(0, second);
        chain.Move(0, 1);
        chain.Remove(first);
        chain.Clear();
        Assert.Equal(new[] { NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Move, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Reset }, actions.Select(x => x.Action));
        Assert.Same(first, actions[0].NewItems![0]);
        Assert.Equal(0, actions[1].NewStartingIndex);
        Assert.Equal(0, actions[2].OldStartingIndex);
        Assert.Equal(1, actions[2].NewStartingIndex);
        Assert.Same(first, actions[3].OldItems![0]);
        Assert.Equal(4, properties.Count(x => x == "Count"));
        Assert.Equal(5, properties.Count(x => x == "Item[]"));
        ((INotifyCollectionChanged)chain).CollectionChanged -= changed;
        ((INotifyPropertyChanged)chain).PropertyChanged -= propertyChanged;
        chain.Add(first);
        Assert.Equal(5, actions.Count);
        Assert.Equal(9, properties.Count);
    }
}
