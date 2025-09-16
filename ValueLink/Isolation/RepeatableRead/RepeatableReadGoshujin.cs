// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// A base interface for Repeatable reads.
/// </summary>
/// <typeparam name="TKey">The type of key class.</typeparam>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public abstract class RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter> : IRepeatableReadSemaphore
    where TObject : class, IRepeatableReadObject<TWriter>, IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, IGoshujin
    where TWriter : class
{
    public abstract Lock LockObject { get; }

    public GoshujinState State { get; set; }

    public int SemaphoreCount { get; set; }

    protected abstract TObject? FindObject(TKey key);

    protected abstract TObject NewObject(TKey key);

    protected async Task<bool> GoshujinStoreData(StoreMode storeMode)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return true;
            }
            else if (storeMode != StoreMode.StoreOnly)
            {// Release
                ((IRepeatableReadSemaphore)this).SetReleasing();
                if (storeMode == StoreMode.TryRelease && this.SemaphoreCount > 0)
                {// Acquired.
                    return false;
                }
            }

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        foreach (var x in array)
        {
            if (x is IStructualObject y && await y.StoreData(storeMode).ConfigureAwait(false) == false)
            {
                return false;
            }
        }

        if (storeMode != StoreMode.StoreOnly)
        {// Released
            using (this.LockObject.EnterScope())
            {
                ((IRepeatableReadSemaphore)this).SetObsolete();
            }
        }

        return true;
    }

    protected async Task GoshujinDeleteData(DateTime forceDeleteAfter)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            ((IRepeatableReadSemaphore)this).SetObsolete();

            var g = this as IGoshujin;
            g?.ClearInternal();
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        foreach (var x in array)
        {
            if (x is IStructualObject y)
            {
                await y.DeleteData(forceDeleteAfter).ConfigureAwait(false);
            }
        }
    }

    public TObject[] GetArray()
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        return array;
    }

    public bool Contains(TKey key)
    {
        using (this.LockObject.EnterScope())
        {
            return this.FindObject(key) != null;
        }
    }

    public bool Contains(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, bool> predicate)
    {
        using (this.LockObject.EnterScope())
        {
            return predicate(this);
        }
    }

    public TObject? TryGet(TKey key)
    {
        using (this.LockObject.EnterScope())
        {
            var x = this.FindObject(key);
            return x;
        }
    }

    public TObject? TryGet(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        using (this.LockObject.EnterScope())
        {
            return predicate(this);
        }
    }

    public TWriter? TryLock(TKey key, AcquisitionMode mode = AcquisitionMode.Get)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = this.FindObject(key);
                if (x is null)
                {// No object
                    if (mode == AcquisitionMode.Get)
                    {// Get
                        ((IRepeatableReadSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                        {
                            return default;
                        }

                        x = this.NewObject(key);
                        TObject.AddToGoshujin(x, (TGoshujin)this, true);
                        goto Created; // Exit using (this.LockObject.EnterScope())
                    }
                }
                else
                {// Exists
                    if (mode == AcquisitionMode.Create)
                    {// Create
                        ((IRepeatableReadSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Get, GetOrCreate
                        if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                        {
                            return default;
                        }

                        // Exit using (this.LockObject.EnterScope())
                    }
                }
            }

            if (x.TryLockInternal(null) is { } writer)
            {
                return writer; // Success (Get)
            }
        }

Created:
        x.WriterSemaphoreInternal.Enter();
        return x.NewWriterInternal(); // Success (Create)
    }

    public ValueTask<TWriter?> TryLockAsync(TKey key, AcquisitionMode mode = AcquisitionMode.Get) => this.TryLockAsync(key, ValueLinkGlobal.LockTimeoutInMilliseconds, default, mode);

    public ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, AcquisitionMode mode = AcquisitionMode.Get) => this.TryLockAsync(key, millisecondsTimeout, default, mode);

    public async ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, CancellationToken cancellationToken, AcquisitionMode mode = AcquisitionMode.Get)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = this.FindObject(key);
                if (x is null)
                {// No object
                    if (mode == AcquisitionMode.Get)
                    {// Get
                        ((IRepeatableReadSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                        {
                            return default;
                        }

                        x = this.NewObject(key);
                        TObject.AddToGoshujin(x, (TGoshujin)this, true);
                        goto Created; // Exit using (this.LockObject.EnterScope())
                    }
                }
                else
                {// Exists
                    if (mode == AcquisitionMode.Create)
                    {// Create
                        ((IRepeatableReadSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Get, GetOrCreate
                        if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                        {
                            return default;
                        }

                        // Exit using (this.LockObject.EnterScope())
                    }
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                    ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                }
                else
                {
                    return x.NewWriterInternal(); // Success (Get)
                }
            }
            else
            {// Timeout/Canceled
                ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                return default;
            }
        }

Created:
        x.WriterSemaphoreInternal.Enter();
        return x.NewWriterInternal(); // Success (Create)
    }

    public TWriter? TryLock(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = predicate(this);
                if (x is null)
                {// No object
                    ((IRepeatableReadSemaphore)this).Release(ref count);
                    return default;
                }
                else
                {// Exists
                    if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                    {
                        return default;
                    }
                }
            }

            if (x.TryLockInternal(null) is { } writer)
            {
                return writer; // Success (Get)
            }
        }
    }

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate) => this.TryLockAsync(predicate, ValueLinkGlobal.LockTimeoutInMilliseconds, default);

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout) => this.TryLockAsync(predicate, millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsync(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = predicate(this);
                if (x is null)
                {// No object
                    ((IRepeatableReadSemaphore)this).Release(ref count);
                    return default;
                }
                else
                {// Exists
                    if (!((IRepeatableReadSemaphore)this).TryAcquire(ref count))
                    {
                        return default;
                    }
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                    ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                }
                else
                {
                    return x.NewWriterInternal(); // Success (Get)
                }
            }
            else
            {// Timeout
                ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                return default;
            }
        }
    }
}
