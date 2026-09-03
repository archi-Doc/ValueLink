// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// Provides state and writer-lock operations for generated repeatable-read records.
/// </summary>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public interface IRepeatableReadObject<TWriter>
    where TWriter : class
{
    /// <summary>
    /// Gets the record's creation, publication, or invalidation state.
    /// </summary>
    RepeatableReadObjectState State { get; }

    /// <summary>
    /// Gets the lock protecting the record's owner.
    /// </summary>
    Lock GoshujinLockObjectInternal { get; }

    /// <summary>
    /// Gets the semaphore that permits one writer at a time.
    /// </summary>
    SemaphoreLock WriterSemaphoreInternal { get; }

    /// <summary>
    /// Creates a writer after the caller acquires the writer semaphore and any owner reference.
    /// </summary>
    /// <returns>A writer whose lock must be released by disposal.</returns>
    TWriter NewWriterInternal();

    /// <summary>
    /// Waits for a writer lock, returning null if the owner or record is invalid.
    /// </summary>
    /// <param name="semaphore">The owner acquisition counter, or null when the caller already owns the required reference.</param>
    /// <returns>A writer to dispose after use, or null if acquisition is unavailable.</returns>
    public TWriter? TryLockInternal(IRepeatableReadSemaphore? semaphore)
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

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or invalid state. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="semaphore">The owner acquisition counter, or null when the caller already owns the required reference.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableReadSemaphore? semaphore)
        => this.TryLockAsyncInternal(semaphore, ValueLinkGlobal.LockTimeoutInMilliseconds, default);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or invalid state. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="semaphore">The owner acquisition counter, or null when the caller already owns the required reference.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableReadSemaphore? semaphore, int millisecondsTimeout)
        => this.TryLockAsyncInternal(semaphore, millisecondsTimeout, default);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or invalid state. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="semaphore">The owner acquisition counter, or null when the caller already owns the required reference.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public async ValueTask<TWriter?> TryLockAsyncInternal(IRepeatableReadSemaphore? semaphore, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (semaphore?.LockAndTryAcquireOne() == false)
        {
            return null;
        }

        bool entered;
        try
        {
            entered = await this.WriterSemaphoreInternal.EnterAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            semaphore?.LockAndReleaseOne();
            throw;
        }

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
