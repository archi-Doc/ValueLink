// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Provides a fixture for tests of serializable storage operations and lock release.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableEntry : IStructuralObject
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; set; }
    public int StoreCalls { get; private set; }
    public int DeleteCalls { get; private set; }
    public StoreMode LastStoreMode { get; private set; }
    public bool LockedDuringStore { get; private set; }
    public bool StoreSucceeds { get; set; } = true;
    public Exception? StoreError { get; set; }
    public bool DeleteJournal { get; private set; }
    public DateTime DeleteDeadline { get; private set; }
    public IStructuralRoot? StructuralRoot { get; set; }
    public IStructuralObject? StructuralParent { get; set; }
    public int StructuralKey { get; set; }

    public Task<bool> StoreData(StoreMode mode)
    {
        this.StoreCalls++;
        this.LastStoreMode = mode;
        this.LockedDuringStore = this.Goshujin!.LockObject.IsLocked;
        return this.StoreError is { } error ? Task.FromException<bool>(error) : Task.FromResult(this.StoreSucceeds);
    }

    public Task DeleteData(DateTime forceDeleteAfter, bool writeJournal)
    {
        this.DeleteCalls++;
        this.DeleteJournal = writeJournal;
        this.DeleteDeadline = forceDeleteAfter;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Exposes storage lifecycle operations for serializable isolation tests.
    /// </summary>
    public partial class GoshujinClass
    {
        public Task<bool> StoreAll(StoreMode mode) => this.GoshujinStoreData(mode);
        public Task DeleteAll(DateTime deadline, bool journal) => this.GoshujinDeleteData(deadline, journal);
    }
}

/// <summary>
/// Provides a list primary chain for snapshot tests.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableListSnapshot
{
    public int Id { get; set; }

    [Link(Type = ChainType.List, Name = "Items", Primary = true)]
    public SerializableListSnapshot(int id) => this.Id = id;
}

/// <summary>
/// Provides a linked-list primary chain for snapshot tests.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableLinkedSnapshot
{
    public int Id { get; set; }

    [Link(Type = ChainType.LinkedList, Name = "Items", Primary = true)]
    public SerializableLinkedSnapshot(int id) => this.Id = id;
}

/// <summary>
/// Provides an observable primary chain for snapshot tests.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableObservableSnapshot
{
    public int Id { get; set; }

    [Link(Type = ChainType.Observable, Name = "Items", Primary = true)]
    public SerializableObservableSnapshot(int id) => this.Id = id;
}

/// <summary>
/// Provides a reverse-ordered primary chain for snapshot tests.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableReverseSnapshot
{
    [Link(Type = ChainType.ReverseOrdered, Primary = true)]
    public int Id { get; set; }
}

/// <summary>
/// Tests serializable storage operations and lock release.
/// </summary>
public class SerializableContractTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void SnapshotsCopyEveryPrimaryChainKindInOrder(int count)
    {
        var ids = Enumerable.Range(0, count).ToArray();

        var list = new SerializableListSnapshot.GoshujinClass();
        var linked = new SerializableLinkedSnapshot.GoshujinClass();
        var observable = new SerializableObservableSnapshot.GoshujinClass();
        var reverse = new SerializableReverseSnapshot.GoshujinClass();
        foreach (var id in ids)
        {
            list.Add(new(id));
            linked.Add(new(id));
            observable.Add(new(id));
            reverse.Add(new() { Id = id });
        }

        Assert.Equal(list.ItemsChain, list.GetArray());
        Assert.Equal(ids, list.GetArray().Select(x => x.Id));
        Assert.Equal(ids, linked.GetArray().Select(x => x.Id));
        Assert.Equal(ids, observable.GetArray().Select(x => x.Id));
        Assert.Equal(Enumerable.Reverse(ids), reverse.GetArray().Select(x => x.Id));
        Assert.All(new[] { list.LockObject, linked.LockObject, observable.LockObject, reverse.LockObject }, x => Assert.False(x.IsLocked));
    }

    [Theory]
    [InlineData(StoreMode.StoreOnly)]
    [InlineData(StoreMode.TryRelease)]
    [InlineData(StoreMode.ForceRelease)]
    public async Task StoreVisitsChildrenAndReleasesTheSemaphoreOnFailure(StoreMode mode)
    {
        var owner = new SerializableEntry.GoshujinClass();
        var first = new SerializableEntry { Id = 1, Goshujin = owner };
        var second = new SerializableEntry { Id = 2, Goshujin = owner, StoreSucceeds = false };
        Assert.False(await owner.StoreAll(mode));
        Assert.Equal(1, first.StoreCalls);
        Assert.Equal(1, second.StoreCalls);
        Assert.Equal(mode, first.LastStoreMode);
        Assert.Equal(mode != StoreMode.StoreOnly, first.LockedDuringStore);
        Assert.False(owner.LockObject.IsLocked);
        second.StoreSucceeds = true;
        Assert.True(await owner.StoreAll(mode));
        Assert.False(owner.LockObject.IsLocked);
        Assert.Equal(2, second.StoreCalls);
    }

    [Theory]
    [InlineData(StoreMode.StoreOnly)]
    [InlineData(StoreMode.TryRelease)]
    [InlineData(StoreMode.ForceRelease)]
    public async Task StoreExceptionPropagatesWithoutHoldingTheSemaphore(StoreMode mode)
    {
        var owner = new SerializableEntry.GoshujinClass();
        var error = new InvalidOperationException("storage failure");
        var item = new SerializableEntry { Goshujin = owner, StoreError = error };
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => owner.StoreAll(mode)));
        Assert.False(owner.LockObject.IsLocked);
        item.StoreError = null;
        Assert.True(await owner.StoreAll(mode));
    }

    [Fact]
    public async Task DeleteUsesASnapshotAndForwardsJournalPolicy()
    {
        var owner = new SerializableEntry.GoshujinClass();
        var first = new SerializableEntry { Id = 1, Goshujin = owner };
        var snapshot = owner.GetArray();
        var second = new SerializableEntry { Id = 2, Goshujin = owner };
        Assert.Same(first, Assert.Single(snapshot));
        await owner.DeleteAll(DateTime.UnixEpoch, false);
        Assert.Empty(owner.GetArray());
        Assert.All(new[] { first, second }, item =>
        {
            Assert.Equal(1, item.DeleteCalls);
            Assert.False(item.DeleteJournal);
            Assert.Equal(DateTime.UnixEpoch, item.DeleteDeadline);
        });
        Assert.False(owner.LockObject.IsLocked);
    }
}
