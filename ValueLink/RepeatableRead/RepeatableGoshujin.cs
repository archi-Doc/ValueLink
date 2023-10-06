// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// A base interface for Repeatable reads.
/// </summary>
/// <typeparam name="TKey">The type of key class.</typeparam>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public abstract class RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>
    where TObject : class, IRepeatableObject<TWriter>, IValueLinkObjectInternal<TGoshujin>
    where TGoshujin : RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>
    where TWriter : class
{
    public abstract object SyncObject { get; }

    public abstract RepeatableGoshujinState State { get; }

    public abstract int LockCount { get; internal set; }

    protected abstract TObject? FindFirst(TKey key);

    protected abstract TObject NewObject(TKey key);

    public TObject[] GetArray()
    {
        TObject[] array;
        lock (this.SyncObject)
        {
            if (this is IEnumerable<TObject> e)
            {
                array = e.ToArray();
            }
            else
            {
                array = Array.Empty<TObject>();
            }
        }

        return array;
    }

    public bool Contains(TKey key)
    {
        lock (this.SyncObject)
        {
            return this.FindFirst(key) != null;
        }
    }

    public bool Contains(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, bool> predicate)
    {
        lock (this.SyncObject)
        {
            return predicate(this);
        }
    }

    public TObject? TryGet(TKey key)
    {
        lock (this.SyncObject)
        {
            var x = this.FindFirst(key);
            return x;
        }
    }

    public TObject? TryGet(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        lock (this.SyncObject)
        {
            return predicate(this);
        }
    }

    public TWriter? TryLock(TKey key, TryLockMode mode = TryLockMode.Get)
    {
        TObject? x = default;
        while (true)
        {
            lock (this.SyncObject)
            {
                if (this.State.IsInvalid())
                {
                    return null;
                }

                x = this.FindFirst(key);
                if (x is null)
                {// No object
                    if (mode == TryLockMode.Get)
                    {// Get
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        x = this.NewObject(key);
                        x.AddToGoshujinInternal((TGoshujin)this);
                        goto Created;
                    }
                }
                else
                {// Exists
                    if (mode == TryLockMode.Create)
                    {// Create
                        return default;
                    }

                    // Get, GetOrCreate
                }
            }

            if (x.TryLockInternal() is { } writer)
            {
                return writer;
            }
        }

Created:
        x.WriterSemaphoreInternal.Enter();
        return x.NewWriterInternal();
    }

    public ValueTask<TWriter?> TryLockAsync(TKey key, TryLockMode mode = TryLockMode.Get) => this.TryLockAsync(key, ValueLinkGlobal.LockTimeout, default, mode);

    public ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, TryLockMode mode = TryLockMode.Get) => this.TryLockAsync(key, millisecondsTimeout, default, mode);

    public async ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, CancellationToken cancellationToken, TryLockMode mode = TryLockMode.Get)
    {
        TObject? x = default;
        while (true)
        {
            lock (this.SyncObject)
            {
                if (this.State.IsInvalid())
                {
                    return null;
                }

                x = this.FindFirst(key);
                if (x is null)
                {// No object
                    if (mode == TryLockMode.Get)
                    {// Get
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        x = this.NewObject(key);
                        x.AddToGoshujinInternal((TGoshujin)this);
                        goto Created;
                    }
                }
                else
                {// Exists
                    if (mode == TryLockMode.Create)
                    {// Create
                        return default;
                    }

                    // Get, GetOrCreate
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                }
                else
                {
                    return x.NewWriterInternal();
                }
            }
            else
            {// Timeout
                return default;
            }

            /*if (await x.TryLockAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false) is { } writer)
            {
                return writer;
            }*/
        }

Created:
        x.WriterSemaphoreInternal.Enter();
        return x.NewWriterInternal();
    }

    public TWriter? TryLock(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        TObject? x = default;
        while (true)
        {
            lock (this.SyncObject)
            {
                if (this.State.IsInvalid())
                {
                    return null;
                }

                x = predicate(this);
                if (x is null)
                {
                    return default;
                }
            }

            if (x.TryLockInternal() is { } writer)
            {
                return writer;
            }
        }
    }

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate) => this.TryLockAsync(predicate, ValueLinkGlobal.LockTimeout, default);

    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout) => this.TryLockAsync(predicate, millisecondsTimeout, default);

    public async ValueTask<TWriter?> TryLockAsync(Func<RepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        TObject? x = default;
        while (true)
        {
            lock (this.SyncObject)
            {
                if (this.State.IsInvalid())
                {
                    return null;
                }

                x = predicate(this);
                if (x is null)
                {
                    return default;
                }
            }

            if (await x.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (x.State.IsInvalid())
                {
                    x.WriterSemaphoreInternal.Exit();
                }
                else
                {
                    return x.NewWriterInternal();
                }
            }
            else
            {// Timeout
                return default;
            }
        }
    }
}
