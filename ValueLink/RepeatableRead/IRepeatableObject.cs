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

    public TWriter? TryLockInternal()
    {
#if DEBUG
        if (Monitor.IsEntered(this.GoshujinSyncObjectInternal))
        {
            throw new LockOrderException();
        }
#endif

        this.WriterSemaphoreInternal.Enter();
        if (this.State.IsInvalid())
        {
            this.WriterSemaphoreInternal.Exit();
            return null;
        }

        return this.NewWriterInternal();
    }

    ValueTask<TWriter?> TryLockAsyncInternal()
        => this.TryLockAsyncInternal(ValueLinkGlobal.LockTimeout, default);

    ValueTask<TWriter?> TryLockAsyncInternal(int millisecondsTimeout)
        => this.TryLockAsyncInternal(millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsyncInternal(int millisecondsTimeout, CancellationToken cancellationToken)
    {
#if DEBUG
        if (Monitor.IsEntered(this.GoshujinSyncObjectInternal))
        {
            throw new LockOrderException();
        }
#endif

        var entered = await this.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false);
        if (!entered)
        {
            return null;
        }
        else if (this.State.IsInvalid())
        {
            this.WriterSemaphoreInternal.Exit();
            return null;
        }
        else
        {
            return this.NewWriterInternal();
        }
    }
}
