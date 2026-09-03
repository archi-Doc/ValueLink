// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

[TinyhandObject]
[ValueLinkObject]
public partial class PersistedItem
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Name = "Ordered", AddValue = true)]
    [Link(Type = ChainType.ReverseOrdered, Name = "Reverse")]
    [Link(Type = ChainType.Unordered, Name = "Hash")]
    public int Id { get; set; }

    [Key(1)]
    public string Text { get; set; } = string.Empty;

    [Link(Type = ChainType.List, Name = "List", Primary = true)]
    [Link(Type = ChainType.LinkedList, Name = "Linked")]
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    [Link(Type = ChainType.StackList, Name = "Stack")]
    [Link(Type = ChainType.Observable, Name = "Observable")]
    public PersistedItem()
    {
    }
}

public class SerializationContractTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(257)]
    public void RoundTripRestoresEveryChainAndItsOwner(int count)
    {
        var original = Create(count);
        var bytes = TinyhandSerializer.Serialize(original);
        var restored = TinyhandSerializer.Deserialize<PersistedItem.GoshujinClass>(bytes)!;
        Assert.NotSame(original, restored);
        Assert.Equal(count, restored.Count);
        Assert.Equal(original.ListChain.Select(x => x.Id), restored.ListChain.Select(x => x.Id));
        Assert.Equal(original.LinkedChain.Select(x => x.Id), restored.LinkedChain.Select(x => x.Id));
        Assert.Equal(original.QueueChain.Select(x => x.Id), restored.QueueChain.Select(x => x.Id));
        Assert.Equal(original.StackChain.Select(x => x.Id), restored.StackChain.Select(x => x.Id));
        Assert.Equal(original.ObservableChain.Select(x => x.Id), restored.ObservableChain.Select(x => x.Id));
        Assert.Equal(original.OrderedChain.Keys, restored.OrderedChain.Keys);
        Assert.Equal(original.ReverseChain.Keys, restored.ReverseChain.Keys);
        Assert.Equal(original.HashChain.Keys.Order(), restored.HashChain.Keys.Order());
        Assert.All(restored, item =>
        {
            Assert.Same(restored, item.Goshujin);
            Assert.Equal($"項目 {item.Id} / \0 / 🚀", item.Text);
            Assert.True(item.ListLink.IsLinked && item.LinkedLink.IsLinked && item.QueueLink.IsLinked && item.StackLink.IsLinked && item.ObservableLink.IsLinked);
            Assert.True(item.OrderedLink.IsLinked && item.ReverseLink.IsLinked && item.HashLink.IsLinked);
            Assert.Same(item, restored.HashChain.FindFirst(item.Id));
            Assert.NotSame(original.HashChain.FindFirst(item.Id), item);
        });

        if (count > 0)
        {
            var item = restored.ListChain[0];
            var oldId = item.Id;
            item.OrderedValue = -1;
            Assert.Null(restored.HashChain.FindFirst(oldId));
            Assert.Same(item, restored.OrderedChain.FindFirst(-1));
            Assert.NotNull(original.HashChain.FindFirst(oldId));
            restored.Remove(item);
            Assert.Null(item.Goshujin);
            Assert.Equal(count - 1, restored.Count);
            Assert.Equal(count - 1, restored.ObservableChain.Count);
        }
    }

    [Fact]
    public void RoundTripPreservesIndependentMembershipAndCustomOrdering()
    {
        var original = Create(8);
        original.LinkedChain.AddFirst(original.ListChain[5]);
        original.QueueChain.Enqueue(original.ListChain[2]);
        original.StackChain.Push(original.ListChain[1]);
        original.ObservableChain.Move(0, 4);
        var omitted = original.ListChain[3];
        original.HashChain.Remove(omitted);
        original.LinkedChain.Remove(omitted);
        original.ObservableChain.Remove(omitted);
        var restored = TinyhandSerializer.Deserialize<PersistedItem.GoshujinClass>(TinyhandSerializer.Serialize(original))!;
        Assert.Equal(original.LinkedChain.Select(x => x.Id), restored.LinkedChain.Select(x => x.Id));
        Assert.Equal(original.QueueChain.Select(x => x.Id), restored.QueueChain.Select(x => x.Id));
        Assert.Equal(original.StackChain.Select(x => x.Id), restored.StackChain.Select(x => x.Id));
        Assert.Equal(original.ObservableChain.Select(x => x.Id), restored.ObservableChain.Select(x => x.Id));
        var restoredOmitted = restored.OrderedChain.FindFirst(omitted.Id)!;
        Assert.Same(restored, restoredOmitted.Goshujin);
        Assert.False(restoredOmitted.HashLink.IsLinked);
        Assert.False(restoredOmitted.LinkedLink.IsLinked);
        Assert.False(restoredOmitted.ObservableLink.IsLinked);
        restored.HashChain.Add(restoredOmitted.Id, restoredOmitted);
        Assert.Same(restoredOmitted, restored.HashChain.FindFirst(omitted.Id));
    }

    [Fact]
    public void CloneCreatesIndependentObjectsAndIndexes()
    {
        var original = Create(5);
        var clone = TinyhandSerializer.Clone(original)!;
        Assert.Equal(original.OrderedChain.Keys, clone.OrderedChain.Keys);
        foreach (var item in clone)
        {
            Assert.NotSame(original.HashChain.FindFirst(item.Id), item);
            Assert.Same(clone, item.Goshujin);
        }

        clone.ClearAll();
        Assert.Empty(clone);
        Assert.Equal(5, original.Count);
        Assert.All(original, item => Assert.Same(original, item.Goshujin));
    }

    private static PersistedItem.GoshujinClass Create(int count)
    {
        var owner = new PersistedItem.GoshujinClass();
        foreach (var id in Enumerable.Range(0, count).Reverse())
        {
            owner.Add(new PersistedItem { Id = id, Text = $"項目 {id} / \0 / 🚀" });
        }

        return owner;
    }
}
