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

    public TObject? TryGet(TKey key)
    {
        lock (this.SyncObject)
        {
            var x = this.FindFirst(key);
            return x;
        }
    }

    public TWriter? TryLock(TKey key, TryLockMode mode = TryLockMode.Get)
    {
        while (true)
        {
            TObject? x = default;
            lock (this.SyncObject)
            {
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
    }

    public ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, TryLockMode mode = TryLockMode.Get) => this.TryLockAsync(key, millisecondsTimeout, default, mode);

    public async ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, CancellationToken cancellationToken, TryLockMode mode = TryLockMode.Get)
    {
        while (true)
        {
            TObject? x = default;
            lock (this.SyncObject)
            {
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

            /*if (x.WriterSemaphore.TryFastEnter())
            {
                if (x.IsObsolete)
                {
                    x.WriterSemaphore.Exit();
                    continue;
                }
                else
                {
                    return x.NewWriter();
                }
            }*/

            if (await x.WriterSemaphoreInternal.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (x.IsObsolete)
                {
                    x.WriterSemaphoreInternal.Exit();
                }
                else
                {
                    return x.NewWriterInternal();
                }
            }

            /*if (await x.TryLockAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false) is { } writer)
            {
                return writer;
            }*/
        }
    }
}
