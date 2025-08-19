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

    Lock GoshujinLockObjectInternal { get; }

    SemaphoreLock WriterSemaphoreInternal { get; }

    TWriter NewWriterInternal();

    public TWriter? TryLockInternal(IRepeatableSemaphore? semaphore)
    {
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

    ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableSemaphore? semaphore)
        => this.TryLockAsyncInternal(semaphore, ValueLinkGlobal.LockTimeoutInMilliseconds, default);

    ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableSemaphore? semaphore, int millisecondsTimeout)
        => this.TryLockAsyncInternal(semaphore, millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableSemaphore? semaphore, int millisecondsTimeout, CancellationToken cancellationToken)
    {
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
