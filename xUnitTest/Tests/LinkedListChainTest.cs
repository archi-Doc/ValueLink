// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

/// <summary>
/// Provides a fixture for tests of linked-list ordering and membership.
/// </summary>
[ValueLinkObject]
public partial class LinkedListChainTestClass
{
    [Link(Primary = true, Type = ChainType.LinkedList, Name = "List")]
    [Link(Type = ChainType.QueueList, Name = "Queue", AddValue = false)]
    [Link(Type = ChainType.StackList, Name = "Stack", AddValue = false)]
    public int Id { get; set; }

    public LinkedListChainTestClass(int id)
    {
        this.Id = id;
    }
}

/// <summary>
/// Tests linked-list ordering and membership.
/// </summary>
public class LinkedListChainTest
{
    [Fact]
    public void AddFirst_InsertsAtTheHead()
    {
        var g = new LinkedListChainTestClass.GoshujinClass();
        var tc1 = new LinkedListChainTestClass(1) { Goshujin = g, };
        var tc2 = new LinkedListChainTestClass(2) { Goshujin = g, };

        // Both objects were appended by the Goshujin setter; put a fresh one at the head.
        var tc3 = new LinkedListChainTestClass(3) { Goshujin = g, };
        g.ListChain.Remove(tc3);
        g.ListChain.AddFirst(tc3);

        g.ListChain.Select(x => x.Id).SequenceEqual([3, 1, 2,]).IsTrue();
        g.ListChain.First!.Id.Is(3);
        g.ListChain.Last!.Id.Is(2);

        // An object already in the chain moves to the head.
        g.ListChain.AddFirst(tc2);
        g.ListChain.Select(x => x.Id).SequenceEqual([2, 3, 1,]).IsTrue();
    }

    [Fact]
    public void TryAddFirst_InsertsAtTheHead()
    {
        var g = new LinkedListChainTestClass.GoshujinClass();
        var tc1 = new LinkedListChainTestClass(1) { Goshujin = g, };
        var tc2 = new LinkedListChainTestClass(2) { Goshujin = g, };
        g.ListChain.Remove(tc2);

        g.ListChain.TryAddFirst(tc2);
        g.ListChain.Select(x => x.Id).SequenceEqual([2, 1,]).IsTrue();

        // Already linked: the position must not change.
        g.ListChain.TryAddFirst(tc1);
        g.ListChain.Select(x => x.Id).SequenceEqual([2, 1,]).IsTrue();
    }

    [Fact]
    public void Clear_UnlinksEveryObject()
    {
        var g = new LinkedListChainTestClass.GoshujinClass();
        var objects = Enumerable.Range(0, 16).Select(x => new LinkedListChainTestClass(x) { Goshujin = g, }).ToArray();
        foreach (var x in objects)
        {
            g.QueueChain.Enqueue(x);
            g.StackChain.Push(x);
        }

        g.ListChain.Count.Is(16);
        g.QueueChain.Count.Is(16);
        g.StackChain.Count.Is(16);

        g.ListChain.Clear();
        g.QueueChain.Clear();
        g.StackChain.Clear();

        g.ListChain.Count.Is(0);
        g.QueueChain.Count.Is(0);
        g.StackChain.Count.Is(0);
        foreach (var x in objects)
        {
            x.ListLink.IsLinked.IsFalse();
            x.QueueLink.IsLinked.IsFalse();
            x.StackLink.IsLinked.IsFalse();
        }
    }

    [Fact]
    public void Enqueue_And_Push_MoveExistingObjects()
    {
        var g = new LinkedListChainTestClass.GoshujinClass();
        var tc1 = new LinkedListChainTestClass(1) { Goshujin = g, };
        var tc2 = new LinkedListChainTestClass(2) { Goshujin = g, };
        var tc3 = new LinkedListChainTestClass(3) { Goshujin = g, };

        foreach (var x in new[] { tc1, tc2, tc3, })
        {
            g.QueueChain.Enqueue(x);
            g.StackChain.Push(x);
        }

        // Re-enqueueing an existing object moves it to the tail.
        g.QueueChain.Enqueue(tc1);
        g.QueueChain.Select(x => x.Id).SequenceEqual([2, 3, 1,]).IsTrue();
        g.QueueChain.Count.Is(3);
        g.QueueChain.Dequeue().Id.Is(2);

        // Re-pushing an existing object moves it to the top.
        g.StackChain.Push(tc1);
        g.StackChain.Select(x => x.Id).SequenceEqual([2, 3, 1,]).IsTrue();
        g.StackChain.Count.Is(3);
        g.StackChain.Pop().Id.Is(1);
    }
}
