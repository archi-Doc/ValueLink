// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// A base interface for Repeatable reads.
/// </summary>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public interface IRepeatableObject<TWriter>
    where TWriter : class
{
    RepeatableObjectState State { get; }

    object GoshujinSyncObjectInternal { get; }

    SemaphoreLock WriterSemaphoreInternal { get; }

    TWriter NewWriterInternal();

    public TWriter? TryLockInternal(IGoshujinSemaphore? semaphore)
    {
#if DEBUG
        if (Monitor.IsEntered(this.GoshujinSyncObjectInternal))
        {
            throw new LockOrderException();
        }
#endif

        if (semaphore?.LockAndTryAcquireOne() == false)
        {
            return null;
        }

        this.WriterSemaphoreInternal.Enter();
        if (this.State.IsInvalid())
        {
            this.WriterSemaphoreInternal.Exit();
            semaphore?.LockAndReleaseOne();
            return null;
        }

        return this.NewWriterInternal();
    }

    ValueTask<TWriter?> TryLockAsyncInternal(IGoshujinSemaphore? semaphore)
        => this.TryLockAsyncInternal(semaphore, ValueLinkGlobal.LockTimeout, default);

    ValueTask<TWriter?> TryLockAsyncInternal(IGoshujinSemaphore? semaphore, int millisecondsTimeout)
        => this.TryLockAsyncInternal(semaphore, millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsyncInternal(IGoshujinSemaphore? semaphore, int millisecondsTimeout, CancellationToken cancellationToken)
    {
#if DEBUG
        if (Monitor.IsEntered(this.GoshujinSyncObjectInternal))
        {
            throw new LockOrderException();
        }
#endif

        if (semaphore?.LockAndTryAcquireOne() == false)
        {
            return null;
        }

        var entered = await this.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false);
        if (!entered)
        {
            semaphore?.LockAndReleaseOne();
            return null;
        }
        else if (this.State.IsInvalid())
        {
            this.WriterSemaphoreInternal.Exit();
            semaphore?.LockAndReleaseOne();
            return null;
        }
        else
        {
            return this.NewWriterInternal();
        }
    }
}
