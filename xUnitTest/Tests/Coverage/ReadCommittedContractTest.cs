// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Tests read-committed acquisition and storage lifecycle contracts.
/// </summary>
public class ReadCommittedContractTest
{
    [Theory]
    [InlineData(AcquisitionMode.GetOnly, false)]
    [InlineData(AcquisitionMode.GetOnlyIgnoreState, false)]
    [InlineData(AcquisitionMode.CreateOnly, true)]
    [InlineData(AcquisitionMode.GetOrCreate, true)]
    public void FindHonorsAcquisitionModeForMissingAndExistingObjects(AcquisitionMode mode, bool creates)
    {
        var owner = new Owner();
        var found = owner.Find(3, mode);
        Assert.Equal(creates, found is not null);
        Assert.Equal(creates ? 1 : 0, owner.NewCalls);
        var existing = owner.Find(3, AcquisitionMode.GetOrCreate)!;
        Assert.Equal(mode == AcquisitionMode.CreateOnly ? null : existing, owner.Find(3, mode));
        Assert.Equal(1, owner.NewCalls);
        Assert.True(existing.AddJournal);
    }

    [Theory]
    [InlineData(AcquisitionMode.GetOnly, DataScopeResult.NotFound)]
    [InlineData(AcquisitionMode.GetOnlyIgnoreState, DataScopeResult.NotFound)]
    [InlineData(AcquisitionMode.CreateOnly, DataScopeResult.Created)]
    [InlineData(AcquisitionMode.GetOrCreate, DataScopeResult.Created)]
    public async Task LockHonorsAcquisitionMode(AcquisitionMode mode, DataScopeResult expected)
    {
        var owner = new Owner();
        using var scope = await owner.TryLock(1, mode, TestContext.Current.CancellationToken);
        Assert.Equal(expected, scope.Result);
        var existing = owner.Find(1, AcquisitionMode.GetOrCreate)!;
        var previousCalls = existing.LockCalls;
        using var duplicate = await owner.TryLock(1, AcquisitionMode.CreateOnly, TestContext.Current.CancellationToken);
        Assert.Equal(DataScopeResult.AlreadyExists, duplicate.Result);
        Assert.Equal(previousCalls, existing.LockCalls);
    }

    [Fact]
    public async Task TimeoutsTokensAndFactoriesReachTheDataLocker()
    {
        var owner = new Owner();
        var point = owner.Find(1, AcquisitionMode.CreateOnly)!;
        var timeout = TimeSpan.FromMilliseconds(123);
        using var cancellation = new CancellationTokenSource();
        Assert.Equal("data", await owner.TryGet(1, timeout, cancellation.Token));
        Assert.Equal(timeout, point.LastTimeout);
        Assert.Equal(cancellation.Token, point.LastToken);
        Func<IStructuralObject, string> factory = _ => "created";
        using var scope = await owner.TryLock(1, AcquisitionMode.GetOrCreate, timeout, cancellation.Token, factory);
        Assert.Same(factory, point.LastFactory);
        Assert.Equal(timeout, point.LastTimeout);
        Assert.Equal(cancellation.Token, point.LastToken);
        Assert.Equal(AcquisitionMode.GetOrCreate, point.LastMode);
        Assert.Null(await owner.TryGet(999, cancellation.Token));
    }

    [Fact]
    public async Task ObsoleteOwnerRejectsReadsAndCreationAndSnapshotsStayIndependent()
    {
        var owner = new Owner();
        var point = owner.Find(1, AcquisitionMode.CreateOnly)!;
        var snapshot = owner.GetArray();
        owner.Find(2, AcquisitionMode.CreateOnly);
        Assert.Same(point, Assert.Single(snapshot));
        var visited = new List<int>();
        owner.ForEach(x =>
        {
            Assert.True(owner.LockObject.IsHeldByCurrentThread);
            visited.Add(x.Key);
        });
        Assert.Equal(new[] { 1, 2 }, visited);
        owner.SetObsolete();
        Assert.False(owner.IsValid);
        Assert.Null(owner.Find(3, AcquisitionMode.CreateOnly));
        Assert.Null(await owner.TryGet(1, TestContext.Current.CancellationToken));
        using var scope = await owner.TryLock(1, AcquisitionMode.GetOnly, TestContext.Current.CancellationToken);
        Assert.Equal(DataScopeResult.Obsolete, scope.Result);
        Assert.Empty(owner.GetArray());
        owner.ForEach(_ => Assert.Fail("An obsolete owner must not invoke the callback."));
        Assert.Equal(2, owner.NewCalls);
        Assert.Equal(0, point.LockCalls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeleteUnlinksOnceAndForwardsJournalPolicy(bool force, bool writeJournal)
    {
        var owner = new Owner();
        var point = owner.Find(1, AcquisitionMode.CreateOnly)!;
        if (force)
        {
            Assert.True(ObjectProtectionStateHelper.TryProtect(ref point.GetProtectionStateRef()));
        }

        var deadline = force ? DateTime.UnixEpoch : default;
        Assert.Equal(force ? DataScopeResult.ForceDeleted : DataScopeResult.Deleted, await owner.Delete(1, deadline, writeJournal));
        Assert.Empty(owner.GetArray());
        Assert.Equal(ObjectProtectionState.Deleted, (ObjectProtectionState)point.GetProtectionStateRef());
        Assert.Equal(writeJournal, point.RemoveJournal);
        Assert.Equal(writeJournal, point.DeleteJournal);
        Assert.Equal(deadline, point.DeleteDeadline);
        Assert.Equal(1, point.DeleteCalls);
        Assert.Equal(DataScopeResult.NotFound, await owner.Delete(1));
        Assert.Equal(1, point.DeleteCalls);
    }

    [Fact]
    public async Task ProtectedDeletionCompletesAfterProtectionIsReleased()
    {
        var owner = new Owner();
        var point = owner.Find(1, AcquisitionMode.CreateOnly)!;
        Assert.True(ObjectProtectionStateHelper.TryProtect(ref point.GetProtectionStateRef()));
        var deletion = owner.Delete(1);
        try
        {
            Assert.False(deletion.IsCompleted);
            Assert.Same(point, owner.Find(1));
            Assert.Equal(0, point.DeleteCalls);
        }
        finally
        {
            ObjectProtectionStateHelper.TryUnprotect(ref point.GetProtectionStateRef());
        }

        Assert.Equal(DataScopeResult.Deleted, await deletion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(1, point.DeleteCalls);
    }

    [Fact]
    public async Task OwnerStoreAndDeleteVisitEveryDataPoint()
    {
        var owner = new Owner();
        var first = owner.Find(1, AcquisitionMode.CreateOnly)!;
        var second = owner.Find(2, AcquisitionMode.CreateOnly)!;
        second.StoreSucceeds = false;
        Assert.False(await owner.StoreAll(StoreMode.StoreOnly));
        Assert.Equal(1, first.StoreCalls);
        Assert.Equal(1, second.StoreCalls);
        second.StoreSucceeds = true;
        Assert.True(await owner.StoreAll(StoreMode.StoreOnly));
        await owner.DeleteAll(DateTime.UnixEpoch, false);
        Assert.False(owner.IsValid);
        Assert.Empty(owner.Points);
        Assert.Equal(1, first.DeleteCalls);
        Assert.Equal(1, second.DeleteCalls);
        Assert.False(first.DeleteJournal);
        Assert.False(second.DeleteJournal);
    }

    // A deterministic storage adapter isolates the base class contract from any disk engine.
    private sealed class Owner : ReadCommittedGoshujin<int, string, Point, Owner>, IGoshujin
    {
        public override Lock LockObject { get; } = new();
        public Dictionary<int, Point> Points { get; } = new();
        public int NewCalls { get; private set; }
        public void ClearChains() => this.Points.Clear();
        public void ClearAll() => this.Points.Clear();
        public IEnumerable GetEnumerableInternal() => this.Points.Values;
        public Task<bool> StoreAll(StoreMode mode) => this.GoshujinStoreData(mode);
        public Task DeleteAll(DateTime deadline, bool journal) => this.GoshujinDeleteData(deadline, journal);
        protected override Point? FindObject(int key) => this.Points.GetValueOrDefault(key);
        protected override Point NewObject(int key)
        {
            this.NewCalls++;
            return new Point(key);
        }
    }

    private sealed class Point(int key) : IValueLinkObjectInternal<Owner, Point>, IDataLocker<string>, IStructuralObject
    {
        private byte protectionState;
        public int Key { get; } = key;
        public bool AddJournal { get; private set; }
        public bool RemoveJournal { get; private set; }
        public bool DeleteJournal { get; private set; }
        public DateTime DeleteDeadline { get; private set; }
        public int DeleteCalls { get; private set; }
        public int LockCalls { get; private set; }
        public int StoreCalls { get; private set; }
        public bool StoreSucceeds { get; set; } = true;
        public TimeSpan LastTimeout { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public Func<IStructuralObject, string>? LastFactory { get; private set; }
        public AcquisitionMode LastMode { get; private set; }
        public IStructuralRoot? StructuralRoot { get; set; }
        public IStructuralObject? StructuralParent { get; set; }
        public int StructuralKey { get; set; }

        public static void AddToGoshujin(Point obj, Owner? owner, bool writeJournal)
        {
            obj.AddJournal = writeJournal;
            owner?.Points.Add(obj.Key, obj);
        }

        public static bool RemoveFromGoshujin(Point obj, Owner? owner, bool writeJournal)
        {
            obj.RemoveJournal = writeJournal;
            return owner?.Points.Remove(obj.Key) == true;
        }

        public static void SetGoshujin(Point obj, Owner? owner) => obj.StructuralParent = null;
        public ref byte GetProtectionStateRef() => ref this.protectionState;
        public ValueTask<string?> TryGet(TimeSpan timeout, CancellationToken cancellationToken)
        {
            this.LastTimeout = timeout;
            this.LastToken = cancellationToken;
            return ValueTask.FromResult<string?>("data");
        }

        public ValueTask<DataScope<string>> TryLock(AcquisitionMode mode, TimeSpan timeout, CancellationToken cancellationToken, Func<IStructuralObject, string>? factory)
        {
            this.LockCalls++;
            this.LastTimeout = timeout;
            this.LastToken = cancellationToken;
            this.LastFactory = factory;
            this.LastMode = mode;
            var backing = new IsolationPrimitiveTest.ScopeBacking();
            return ValueTask.FromResult(new DataScope<string>(DataScopeResult.Created, "data", backing, this));
        }

        public Task DeletePoint(DateTime forceDeleteAfter, bool writeJournal)
        {
            this.DeleteCalls++;
            this.DeleteDeadline = forceDeleteAfter;
            this.DeleteJournal = writeJournal;
            return Task.CompletedTask;
        }

        public Task<bool> StoreData(StoreMode storeMode)
        {
            this.StoreCalls++;
            return Task.FromResult(this.StoreSucceeds);
        }
    }
}
