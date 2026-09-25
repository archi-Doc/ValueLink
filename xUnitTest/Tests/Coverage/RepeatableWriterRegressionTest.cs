// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using ValueLink;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>
/// Tests writer lifetime and multiple indexes on the same key.
/// </summary>
public class RepeatableWriterRegressionTest
{
    [Fact]
    public void UniqueLinkCanBeTheSecondIndexOnAMember()
    {
        var owner = new MultiIndexRecord.GoshujinClass();
        using var writer = owner.TryLock(42, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        Assert.Equal(42, writer.Id);
        var record = writer.Commit();
        Assert.NotNull(record);
        Assert.Same(record, owner.HashChain.FindFirst(42));
        Assert.Same(record, owner.OrderedChain.FindFirst(42));
    }

    [Fact]
    public void CommitUpdatesEveryIndexOnTheChangedMember()
    {
        var owner = new MultiIndexRecord.GoshujinClass();
        using var writer = owner.TryLock(0, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        var old = writer.Commit();
        writer.Id = 9;
        var updated = writer.Commit();
        Assert.NotNull(updated);
        Assert.NotSame(old, updated);
        Assert.Null(owner.OrderedChain.FindFirst(0));
        Assert.Null(owner.HashChain.FindFirst(0));
        Assert.Same(updated, owner.OrderedChain.FindFirst(9));
        Assert.Same(updated, owner.HashChain.FindFirst(9));
    }

    [Fact]
    public void RevertingTheUniqueKeyDoesNotConflictWithTheOriginalRecord()
    {
        var owner = new TrackedEntry.GoshujinClass();
        using var writer = owner.TryLock(1, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        writer.Commit();
        writer.Id = 2;
        writer.Id = 1;
        writer.Value = 10;
        Assert.NotNull(writer.Commit());
        Assert.Equal(10, owner.TryGet(1)!.Value);
    }

    [Fact]
    public void DisposingAWriterTwiceDoesNotReleaseAnotherWritersLock()
    {
        var owner = new TrackedEntry.GoshujinClass();
        var first = owner.TryLock(1, AcquisitionMode.CreateOnly)!;
        first.Commit();
        first.Dispose();
        using var second = owner.TryLock(1);
        Assert.NotNull(second);
        first.Dispose();
        Assert.Equal(1, owner.AcquisitionCount);
    }

    [Fact]
    public void EmptyCommitAfterDeletionKeepsTheRecordObsolete()
    {
        var owner = new TrackedEntry.GoshujinClass();
        TrackedEntry record, deleted;
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(writer);
            record = writer.Commit()!;
            writer.Delete();
            deleted = writer.Commit()!;
            Assert.Equal(RepeatableReadObjectState.Obsolete, deleted.State);
            Assert.Same(deleted, writer.Commit());
        }

        Assert.Equal(RepeatableReadObjectState.Obsolete, deleted.State);
        Assert.Equal(RepeatableReadObjectState.Obsolete, record.State);
        Assert.Null(deleted.TryLock());
        Assert.Empty(owner.GetArray());
        Assert.Equal(0, owner.AcquisitionCount);
    }

    [Fact]
    public void DisposedWritersCannotPublishOrEditRecords()
    {
        var owner = new TrackedEntry.GoshujinClass();
        var writer = owner.TryLock(1, AcquisitionMode.CreateOnly)!;
        writer.Commit();
        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.Commit());
        Assert.Throws<ObjectDisposedException>(() => writer.Value = 5);
        Assert.Throws<ObjectDisposedException>(() => writer.Rollback());
        Assert.Throws<ObjectDisposedException>(() => writer.Delete());
        Assert.Equal(0, owner.TryGet(1)!.Value);
    }

    [Fact]
    public void SecondaryUniqueIndexRejectsAnotherRecordsKey()
    {
        var owner = new MultiIndexRecord.GoshujinClass();
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly))
        {
            Assert.NotNull(writer);
            writer.Commit();
        }

        using var second = owner.TryLock(2, AcquisitionMode.CreateOnly);
        Assert.NotNull(second);
        second.Commit();
        second.Id = 1;
        Assert.Null(second.Commit());
        Assert.Equal(2, owner.Count);
        Assert.Equal(2, owner.HashChain.FindFirst(2)!.Id);
        second.Id = 3;
        Assert.NotNull(second.Commit());
        Assert.Null(owner.HashChain.FindFirst(2));
        Assert.NotNull(owner.HashChain.FindFirst(3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ThrowingConstructorsReleaseTheOwnerAcquisition(bool asynchronous)
    {
        var owner = new ThrowingConstructorRecord.GoshujinClass();
        if (asynchronous)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using var writer = await owner.TryLockAsync(1, 0, TestContext.Current.CancellationToken, AcquisitionMode.CreateOnly);
            });
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => owner.TryLock(1, AcquisitionMode.CreateOnly));
        }

        Assert.Equal(0, owner.AcquisitionCount);
        Assert.Empty(owner.GetArray());
    }

    [Fact]
    public void ThrowingAddedHookDuringAMoveDoesNotLeakTheTargetAcquisition()
    {
        var source = new ThrowingHookRecord.GoshujinClass();
        var target = new ThrowingHookRecord.GoshujinClass();
        ThrowingHookRecord record;
        using (var writer = source.TryLock(1, AcquisitionMode.CreateOnly)!)
        {
            record = writer.Commit()!;
        }

        using (var writer = record.TryLock()!)
        {
            writer.ThrowOnAdd = true;
            writer.Goshujin = target;
            Assert.Throws<InvalidOperationException>(() => writer.Commit());
        }

        Assert.Equal(0, source.AcquisitionCount);
        Assert.Equal(0, target.AcquisitionCount);
        Assert.True(((IRepeatableReadSemaphore)target).CanRelease);
        Assert.Same(record, source.TryGet(1));
        Assert.Equal(RepeatableReadObjectState.Valid, record.State);
    }

    [Fact]
    public void MovingIntoAnOwnerChecksAUniqueKeyWithoutAWriterMember()
    {
        var source = new IgnoredKeyRecord.GoshujinClass();
        var target = new IgnoredKeyRecord.GoshujinClass();
        using (var writer = source.TryLock(1, AcquisitionMode.CreateOnly)!)
        {
            writer.Commit();
        }

        using (var writer = target.TryLock(1, AcquisitionMode.CreateOnly)!)
        {
            writer.Commit();
        }

        var moving = source.TryGet(1)!;
        Assert.Null(target.Add(moving));
        Assert.Equal(1, target.Count);
        Assert.Same(moving, source.TryGet(1));
        Assert.Equal(0, source.AcquisitionCount);
        Assert.Equal(0, target.AcquisitionCount);
    }

    [Fact]
    public void CustomAccessorsStillExecuteOnTheEditableCopy()
    {
        var owner = new CustomAccessorRecord.GoshujinClass();
        using var writer = owner.TryLock(1, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        var original = writer.Commit()!;
        _ = writer.Value;
        writer.Value = 0;
        Assert.Equal(0, original.Reads);
        Assert.Equal(0, original.Writes);
        var updated = writer.Commit()!;
        Assert.Equal(1, updated.Reads);
        Assert.Equal(1, updated.Writes);
    }

    [Fact]
    public void EqualReferenceValuesCanStillReplaceTheStoredInstance()
    {
        var owner = new MultiIndexRecord.GoshujinClass();
        using var writer = owner.TryLock(1, AcquisitionMode.CreateOnly);
        Assert.NotNull(writer);
        var first = new string('x', 3);
        writer.Text = first;
        writer.Commit();
        var second = new string('x', 3);
        writer.Text = second;
        var updated = writer.Commit()!;
        Assert.NotSame(first, second);
        Assert.Same(second, updated.Text);
    }
}

/// <summary>
/// Exercises a unique index declared after another index on the same member.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record MultiIndexRecord
{
    [Link(Type = ChainType.Ordered, Name = "Ordered", Primary = true)]
    [Link(Type = ChainType.Unordered, Name = "Hash", Unique = true)]
    public int Id { get; private set; }

    public string? Text { get; private set; }
}

/// <summary>
/// Exercises acquisition cleanup when user construction fails.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record ThrowingConstructorRecord
{
    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    public int Id { get; private set; }

    public ThrowingConstructorRecord() => throw new InvalidOperationException("Construction failed.");
}

/// <summary>
/// Exercises custom accessor side effects without mutating published snapshots.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record CustomAccessorRecord
{
    private int value;

    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    public int Id { get; private set; }

    public int Reads { get; private set; }

    public int Writes { get; private set; }

    public int Value
    {
        get { this.Reads++; return this.value; }
        private set { this.Writes++; this.value = value; }
    }
}

/// <summary>
/// Exercises acquisition cleanup when a link hook fails during a move between owners.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record ThrowingHookRecord
{
    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    public int Id { get; private set; }

    public bool ThrowOnAdd { get; private set; }

    private void IdLinkAdded()
    {
        if (this.ThrowOnAdd)
        {
            throw new InvalidOperationException("Hook failed.");
        }
    }
}

/// <summary>
/// Exercises a unique key that is excluded from generated writer members.
/// </summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record IgnoredKeyRecord
{
    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    [Tinyhand.IgnoreMember]
    public int Id { get; private set; }
}
