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
public abstract class RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter> : IRepeatableSemaphore
    where TObject : class, IRepeatableObject<TWriter>, IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, IGoshujin
    where TWriter : class
{
    public abstract Lock LockObject { get; }

    public GoshujinState State { get; set; }

    public int SemaphoreCount { get; set; }

    protected abstract TObject? FindFirst(TKey key);

    protected abstract TObject NewObject(TKey key);

    protected async Task<bool> GoshujinSave(UnloadMode unloadMode)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return true;
            }
            else if (unloadMode != UnloadMode.NoUnload)
            {// TryUnload or ForceUnload
                ((IRepeatableSemaphore)this).SetReleasing();
                if (unloadMode == UnloadMode.TryUnload && this.SemaphoreCount > 0)
                {// Acquired.
                    return false;
                }
            }

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        foreach (var x in array)
        {
            if (x is IStructualObject y && await y.Save(unloadMode).ConfigureAwait(false) == false)
            {
                return false;
            }
        }

        if (unloadMode != UnloadMode.NoUnload)
        {// Unloaded
            using (this.LockObject.EnterScope())
            {
                ((IRepeatableSemaphore)this).SetObsolete();
            }
        }

        return true;
    }

    protected async Task<bool> GoshujinStoreData(StoreMode storeMode)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return true;
            }
            else if (storeMode == StoreMode.Release)
            {// Release
                ((IRepeatableSemaphore)this).SetReleasing();//
                /*if (unloadMode == UnloadMode.TryUnload && this.SemaphoreCount > 0)
                {// Acquired.
                    return false;
                }*/
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

        if (storeMode == StoreMode.Release)
        {// Released
            using (this.LockObject.EnterScope())
            {
                ((IRepeatableSemaphore)this).SetObsolete();
            }
        }

        return true;
    }

    protected void GoshujinErase()
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            ((IRepeatableSemaphore)this).SetObsolete();
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        foreach (var x in array)
        {
            if (x is IStructualObject y)
            {
                y.Erase();
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
            return this.FindFirst(key) != null;
        }
    }

    public bool Contains(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, bool> predicate)
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
            var x = this.FindFirst(key);
            return x;
        }
    }

    public TObject? TryGet(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        using (this.LockObject.EnterScope())
        {
            return predicate(this);
        }
    }

    public TWriter? TryLock(TKey key, TryLockMode mode = TryLockMode.Get)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = this.FindFirst(key);
                if (x is null)
                {// No object
                    if (mode == TryLockMode.Get)
                    {// Get
                        ((IRepeatableSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
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
                    if (mode == TryLockMode.Create)
                    {// Create
                        ((IRepeatableSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Get, GetOrCreate
                        if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
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

    public ValueTask<TWriter?> TryLockAsync(TKey key, TryLockMode mode = TryLockMode.Get) => this.TryLockAsync(key, ValueLinkGlobal.LockTimeout, default, mode);

    public ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, TryLockMode mode = TryLockMode.Get) => this.TryLockAsync(key, millisecondsTimeout, default, mode);

    public async ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, CancellationToken cancellationToken, TryLockMode mode = TryLockMode.Get)
    {
        TObject? x = default;
        int count = 0;
        while (true)
        {
            using (this.LockObject.EnterScope())
            {
                x = this.FindFirst(key);
                if (x is null)
                {// No object
                    if (mode == TryLockMode.Get)
                    {// Get
                        ((IRepeatableSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
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
                    if (mode == TryLockMode.Create)
                    {// Create
                        ((IRepeatableSemaphore)this).Release(ref count);
                        return default;
                    }
                    else
                    {// Get, GetOrCreate
                        if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
                        {
                            return default;
                        }

                        // Exit using (this.LockObject.EnterScope())
                    }
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                    ((IRepeatableSemaphore)this).LockAndRelease(ref count);
                }
                else
                {
                    return x.NewWriterInternal(); // Success (Get)
                }
            }
            else
            {// Timeout/Canceled
                ((IRepeatableSemaphore)this).LockAndRelease(ref count);
                return default;
            }
        }

Created:
        x.WriterSemaphoreInternal.Enter();
        return x.NewWriterInternal(); // Success (Create)
    }

    public TWriter? TryLock(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
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
                    ((IRepeatableSemaphore)this).Release(ref count);
                    return default;
                }
                else
                {// Exists
                    if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
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

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate) => this.TryLockAsync(predicate, ValueLinkGlobal.LockTimeout, default);

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout) => this.TryLockAsync(predicate, millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout, CancellationToken cancellationToken)
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
                    ((IRepeatableSemaphore)this).Release(ref count);
                    return default;
                }
                else
                {// Exists
                    if (!((IRepeatableSemaphore)this).TryAcquire(ref count))
                    {
                        return default;
                    }
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                    ((IRepeatableSemaphore)this).LockAndRelease(ref count);
                }
                else
                {
                    return x.NewWriterInternal(); // Success (Get)
                }
            }
            else
            {// Timeout
                ((IRepeatableSemaphore)this).LockAndRelease(ref count);
                return default;
            }
        }
    }
}
