// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

public class IsolationPrimitiveTest
{
    public static IEnumerable<object[]> ProtectionStates => Enum.GetValues<ObjectProtectionState>().Select(x => new object[] { x });

    public static IEnumerable<object[]> ScopeResults => Enum.GetValues<DataScopeResult>().Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(ProtectionStates))]
    public void ProtectionTransitionsObeyTheStateMachine(ObjectProtectionState initial)
    {
        var state = (byte)initial;
        Assert.Equal(initial == ObjectProtectionState.Unprotected, ObjectProtectionStateHelper.TryProtect(ref state));
        Assert.Equal(initial == ObjectProtectionState.Unprotected ? ObjectProtectionState.Protected : initial, (ObjectProtectionState)state);
        state = (byte)initial;
        ObjectProtectionStateHelper.TryUnprotect(ref state);
        Assert.Equal(initial == ObjectProtectionState.Protected ? ObjectProtectionState.Unprotected : initial, (ObjectProtectionState)state);
        state = (byte)initial;
        Assert.Equal(initial == ObjectProtectionState.Protected, ObjectProtectionStateHelper.TryMarkPendingDeletion(ref state));
        Assert.Equal(initial == ObjectProtectionState.Protected ? ObjectProtectionState.PendingDeletion : initial, (ObjectProtectionState)state);
        state = (byte)initial;
        Assert.Equal(initial != ObjectProtectionState.Protected, ObjectProtectionStateHelper.TryDelete(ref state, out var original));
        Assert.Equal(initial, original);
        Assert.Equal(initial == ObjectProtectionState.Protected ? initial : ObjectProtectionState.Deleted, (ObjectProtectionState)state);
        state = (byte)initial;
        Assert.Equal(initial != ObjectProtectionState.Deleted, ObjectProtectionStateHelper.ForceDelete(ref state));
        Assert.Equal(ObjectProtectionState.Deleted, (ObjectProtectionState)state);
        Assert.False(ObjectProtectionStateHelper.TryProtect(ref state));
        Assert.Equal(initial is ObjectProtectionState.Deleted or ObjectProtectionState.PendingDeletion, ObjectProtectionStateHelper.IsObsolete(initial));
        Assert.Equal(ObjectProtectionStateHelper.IsObsolete(initial), ObjectProtectionStateHelper.IsObsolete((byte)initial));
    }

    [Fact]
    public void OnlyOneConcurrentCallerCanProtectAnObject()
    {
        byte state = 0;
        var winners = 0;
        Parallel.For(0, 1_000, _ =>
        {
            if (ObjectProtectionStateHelper.TryProtect(ref state))
            {
                Interlocked.Increment(ref winners);
            }
        });
        Assert.Equal(1, winners);
        Assert.Equal(ObjectProtectionState.Protected, (ObjectProtectionState)state);
    }

    [Theory]
    [MemberData(nameof(ScopeResults))]
    public async Task EmptyScopeIsSafeToDisposeOrDelete(DataScopeResult result)
    {
        var scope = new DataScope<string>(result);
        Assert.Equal(result, scope.Result);
        Assert.False(scope.IsValid);
        Assert.False(scope.IsCreated);
        Assert.False(scope.IsRetrieved);
        scope.SetControlState(DataControlState.Pinned);
        Assert.Equal(DataControlState.Default, scope.GetControlState());
        scope.Dispose();
        scope.Dispose();
        await scope.UnlockAndDelete();
        Assert.Null(scope.Data);
    }

    [Theory]
    [InlineData(DataScopeResult.Created)]
    [InlineData(DataScopeResult.Retrieved)]
    public void ScopeForwardsStateAndUnlocksExactlyOnce(DataScopeResult result)
    {
        var backing = new ScopeBacking();
        var scope = new DataScope<string>(result, "data", backing, backing);
        Assert.True(scope.IsValid);
        Assert.Equal(result == DataScopeResult.Created, scope.IsCreated);
        Assert.Equal(result == DataScopeResult.Retrieved, scope.IsRetrieved);
        scope.SetControlState(DataControlState.Pinned | DataControlState.NotLockable);
        Assert.Equal(DataControlState.Pinned | DataControlState.NotLockable, scope.GetControlState());
        scope.Dispose();
        scope.Dispose();
        scope.SetControlState(DataControlState.Default);
        Assert.Equal(DataControlState.Pinned | DataControlState.NotLockable, backing.ControlState);
        Assert.Equal(DataControlState.Default, scope.GetControlState());
        Assert.False(scope.IsValid);
        Assert.False(scope.IsCreated);
        Assert.False(scope.IsRetrieved);
        Assert.Equal(new[] { "Unlock" }, backing.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ScopeDeletionInvalidatesDataAndDeletesStructureOnlyOnce(bool canDelete)
    {
        var backing = new ScopeBacking { CanDelete = canDelete };
        var scope = new DataScope<string>(DataScopeResult.Retrieved, "data", backing, backing);
        var deadline = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await scope.UnlockAndDelete(deadline);
        await scope.UnlockAndDelete(deadline);
        scope.Dispose();
        Assert.False(scope.IsValid);
        Assert.Equal(canDelete ? new[] { "UnlockAndDelete", "DeleteData" } : new[] { "UnlockAndDelete" }, backing.Calls);
        if (canDelete)
        {
            Assert.Equal(deadline, backing.Deadline);
            Assert.True(backing.WriteJournal);
        }
    }

    [Fact]
    public void SemaphoreCountsAreBalancedDuringRelease()
    {
        IRepeatableReadSemaphore semaphore = new TestSemaphore();
        var acquired = 0;
        Assert.True(semaphore.CanRelease);
        Assert.True(semaphore.TryAcquire(ref acquired));
        Assert.True(semaphore.TryAcquire(ref acquired));
        Assert.Equal(1, acquired);
        Assert.Equal(1, semaphore.SemaphoreCount);
        Assert.False(semaphore.CanRelease);
        Assert.False(semaphore.LockAndTryRelease(out var state));
        Assert.Equal(GoshujinState.Releasing, state);
        Assert.False(semaphore.TryAcquire(ref acquired));
        Assert.Equal(0, acquired);
        Assert.Equal(0, semaphore.SemaphoreCount);
        Assert.False(semaphore.LockAndTryAcquireOne());
        semaphore.LockAndRelease(ref acquired);
        semaphore.SetObsolete();
        Assert.False(semaphore.IsValid);
        Assert.False(semaphore.LockAndTryRelease(out state));
        Assert.Equal(GoshujinState.Obsolete, state);
    }

    [Fact]
    public void ConcurrentSemaphoreAcquisitionsBalanceAndReleasePreventsNewWriters()
    {
        IRepeatableReadSemaphore semaphore = new TestSemaphore();
        Parallel.For(0, 1_000, _ =>
        {
            Assert.True(semaphore.LockAndTryAcquireOne());
            semaphore.LockAndReleaseOne();
        });
        Assert.Equal(0, semaphore.SemaphoreCount);
        Assert.True(semaphore.LockAndTryRelease(out var state));
        Assert.Equal(GoshujinState.Releasing, state);
        Assert.False(semaphore.LockAndTryAcquireOne());
        Assert.Equal(0, semaphore.SemaphoreCount);
    }

    internal sealed class ScopeBacking : IDataUnlocker, IStructuralObject
    {
        public List<string> Calls { get; } = new();
        public bool CanDelete { get; init; } = true;
        public DataControlState ControlState { get; set; }
        public DateTime Deadline { get; private set; }
        public bool WriteJournal { get; private set; }
        public IStructuralRoot? StructuralRoot { get; set; }
        public IStructuralObject? StructuralParent { get; set; }
        public int StructuralKey { get; set; }
        public void Unlock() => this.Calls.Add("Unlock");
        public bool UnlockAndDelete()
        {
            this.Calls.Add("UnlockAndDelete");
            return this.CanDelete;
        }

        public DataControlState GetControlState() => this.ControlState;
        public void SetControlState(DataControlState state) => this.ControlState = state;
        public Task DeleteData(DateTime forceDeleteAfter, bool writeJournal)
        {
            this.Calls.Add("DeleteData");
            this.Deadline = forceDeleteAfter;
            this.WriteJournal = writeJournal;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSemaphore : IRepeatableReadSemaphore
    {
        public Lock LockObject { get; } = new();
        public GoshujinState State { get; set; }
        public int SemaphoreCount { get; set; }
    }
}
