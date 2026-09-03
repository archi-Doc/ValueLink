// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record TrackedEntry : IStructuralObject
{
    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    public int Id { get; private set; }
    public int Value { get; private set; }
    public int DeleteCalls { get; private set; }
    public bool DeleteJournal { get; private set; }
    public DateTime DeleteDeadline { get; private set; }
    IStructuralRoot? IStructuralObject.StructuralRoot { get; set; }
    IStructuralObject? IStructuralObject.StructuralParent { get; set; }
    int IStructuralObject.StructuralKey { get; set; }

    public Task DeleteData(DateTime forceDeleteAfter, bool writeJournal)
    {
        this.DeleteCalls++;
        this.DeleteJournal = writeJournal;
        this.DeleteDeadline = forceDeleteAfter;
        return Task.CompletedTask;
    }

    public partial class GoshujinClass
    {
        public Task DeleteAll(DateTime deadline, bool journal) => this.GoshujinDeleteData(deadline, journal);
        public Task<bool> StoreAll(StoreMode mode) => this.GoshujinStoreData(mode);
    }
}

public class RepeatableReadContractTest
{
    [Fact]
    public void CommitPublishesANewSnapshotAndDisposalRollsBack()
    {
        var owner = new TrackedEntry.GoshujinClass();
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(writer);
            writer.Value = 10;
            Assert.NotNull(writer.Commit());
        }

        var snapshot = Assert.Single(owner.GetArray());
        using (var writer = owner.TryLock(1))
        {
            Assert.NotNull(writer);
            writer.Value = 20;
            Assert.Equal(10, owner.TryGet(1)!.Value);
        }

        Assert.Same(snapshot, owner.TryGet(1));
        using (var writer = owner.TryLock(1))
        {
            Assert.NotNull(writer);
            writer.Value = 30;
            Assert.NotSame(snapshot, writer.Commit());
        }

        Assert.Equal(10, snapshot.Value);
        Assert.Equal(30, owner.TryGet(1)!.Value);
        Assert.Equal(RepeatableReadObjectState.Obsolete, snapshot.State);
        Assert.Equal(0, owner.SemaphoreCount);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task FailedAsyncAcquisitionDoesNotLeakSemaphoreCounts(int entryPoint, bool cancelWhileWaiting)
    {
        var owner = new TrackedEntry.GoshujinClass();
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(writer);
            writer.Commit();
        }

        using (var held = owner.TryLock(1))
        {
            Assert.NotNull(held);
            using var timeout = await Acquire(0, TestContext.Current.CancellationToken);
            Assert.Null(timeout);
            Assert.Equal(1, owner.SemaphoreCount);
            using var cancellation = new CancellationTokenSource();
            if (!cancelWhileWaiting)
            {
                cancellation.Cancel();
            }

            var acquisition = Acquire(5000, cancellation.Token);
            if (cancelWhileWaiting)
            {
                Assert.False(acquisition.IsCompleted);
                cancellation.Cancel();
            }

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                using var canceled = await acquisition;
            });
            Assert.Equal(1, owner.SemaphoreCount);
        }

        Assert.Equal(0, owner.SemaphoreCount);
        using (var retry = await owner.TryLockAsync(1, 1000, TestContext.Current.CancellationToken))
        {
            Assert.NotNull(retry);
        }

        Assert.Equal(0, owner.SemaphoreCount);

        ValueTask<TrackedEntry.WriterClass?> Acquire(int milliseconds, CancellationToken token) => entryPoint switch
        {
            0 => owner.TryLockAsync(1, milliseconds, token),
            1 => owner.TryLockAsync(g => g.TryGet(1), milliseconds, token),
            _ => owner.TryGet(1)!.TryLockAsync(milliseconds, token),
        };
    }

    [Fact]
    public async Task ConcurrentWritersDoNotLoseCommittedUpdates()
    {
        var owner = new TrackedEntry.GoshujinClass();
        using (var initial = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(initial);
            initial.Commit();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 32).Select(async _ =>
        {
            await start.Task;
            using var writer = await owner.TryLockAsync(1, 5000, TestContext.Current.CancellationToken);
            Assert.NotNull(writer);
            writer.Value++;
            Assert.NotNull(writer.Commit());
        }).ToArray();
        start.SetResult();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(32, owner.TryGet(1)!.Value);
        Assert.Equal(0, owner.SemaphoreCount);
        Assert.Single(owner);
    }

    [Fact]
    public async Task ReleasingOwnerRejectsWritersAndStoresOnceTheyHaveLeft()
    {
        var owner = new TrackedEntry.GoshujinClass();
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(writer);
            writer.Commit();
            Assert.False(await owner.StoreAll(StoreMode.TryRelease));
            Assert.Equal(GoshujinState.Releasing, owner.State);
            using var rejected = await owner.TryLockAsync(2, 0, TestContext.Current.CancellationToken, AcquisitionMode.CreateOnly);
            Assert.Null(rejected);
        }

        Assert.Equal(0, owner.SemaphoreCount);
        Assert.True(await owner.StoreAll(StoreMode.TryRelease));
        Assert.Equal(GoshujinState.Obsolete, owner.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletingOwnerVisitsChildrenBeforeLosingTheirReferences(bool writeJournal)
    {
        var owner = new TrackedEntry.GoshujinClass();
        for (var id = 0; id < 3; id++)
        {
            using var writer = owner.TryLock(id, AcquisitionMode.CreateOnly);
            Assert.NotNull(writer);
            writer.Commit();
        }

        var children = owner.GetArray();
        await owner.DeleteAll(DateTime.UnixEpoch, writeJournal);
        Assert.Empty(owner);
        Assert.Equal(GoshujinState.Obsolete, owner.State);
        Assert.All(children, child =>
        {
            Assert.Equal(1, child.DeleteCalls);
            Assert.Equal(writeJournal, child.DeleteJournal);
            Assert.Equal(DateTime.UnixEpoch, child.DeleteDeadline);
        });
    }
}
