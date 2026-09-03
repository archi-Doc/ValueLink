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
/// Provides keyed access and writer acquisition for a repeatable-read owner.
/// </summary>
/// <typeparam name="TKey">The type of key class.</typeparam>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
/// <remarks>
/// Writers publish record copies on commit. Mutable reference-type members remain shared unless explicitly copied by the caller.
/// </remarks>
public abstract class RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter> : IRepeatableReadSemaphore
    where TObject : class, IRepeatableReadObject<TWriter>, IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, IGoshujin
    where TWriter : class
{
    /// <summary>
    /// Gets the lock protecting owner state and chains.
    /// </summary>
    public abstract Lock LockObject { get; }

    /// <summary>
    /// Gets or sets the owner's lifecycle state while holding LockObject.
    /// </summary>
    public GoshujinState State { get; set; }

    /// <summary>
    /// Gets or sets the active acquisition count while holding LockObject.
    /// </summary>
    public int SemaphoreCount { get; set; }

    /// <summary>
    /// Finds a record by its unique key while the caller holds LockObject.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <returns>The selected record, or null if no record matches.</returns>
    protected abstract TObject? FindObject(TKey key);

    /// <summary>
    /// Creates an unowned record with the requested unique key.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <returns>A new unowned record with the requested key.</returns>
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
            if (x is IStructuralObject y && await y.StoreData(storeMode).ConfigureAwait(false) == false)
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

    protected async Task GoshujinDeleteData(DateTime forceDeleteAfter, bool writeJournal)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            ((IRepeatableReadSemaphore)this).SetObsolete();

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
            var g = this as IGoshujin;
            g?.ClearChains();
        }

        foreach (var x in array)
        {
            if (x is IStructuralObject y)
            {
                await y.DeleteData(forceDeleteAfter, writeJournal).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Copies the primary chain to an array under the owner lock, or returns an empty array without enumeration support.
    /// </summary>
    /// <returns>A snapshot of primary-chain object references, or an empty array without enumeration support.</returns>
    public TObject[] GetArray()
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        return array;
    }

    /// <summary>
    /// Checks for a matching record under the owner lock.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <returns>True if the key exists or the supplied condition is satisfied; otherwise false.</returns>
    public bool Contains(TKey key)
    {
        using (this.LockObject.EnterScope())
        {
            return this.FindObject(key) != null;
        }
    }

    /// <summary>
    /// Checks for a matching record under the owner lock.
    /// </summary>
    /// <param name="predicate">A condition evaluated under the owner lock.</param>
    /// <returns>True if the key exists or the supplied condition is satisfied; otherwise false.</returns>
    public bool Contains(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, bool> predicate)
    {
        using (this.LockObject.EnterScope())
        {
            return predicate(this);
        }
    }

    /// <summary>
    /// Returns a matching record under the owner lock, or null if none exists.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <returns>The selected record, or null if no record matches.</returns>
    public TObject? TryGet(TKey key)
    {
        using (this.LockObject.EnterScope())
        {
            var x = this.FindObject(key);
            return x;
        }
    }

    /// <summary>
    /// Returns a matching record under the owner lock, or null if none exists.
    /// </summary>
    /// <param name="predicate">A selector evaluated under the owner lock; return only an object owned by this owner.</param>
    /// <returns>The selected record, or null if no record matches.</returns>
    public TObject? TryGet(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate)
    {
        using (this.LockObject.EnterScope())
        {
            return predicate(this);
        }
    }

    /// <summary>
    /// Waits for a writer for a selected record, returning null when acquisition is unavailable.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <param name="mode">Whether to retrieve an existing record, create one, or allow either operation.</param>
    /// <returns>A writer to dispose after use, or null if acquisition is unavailable.</returns>
    public TWriter? TryLock(TKey key, AcquisitionMode mode = AcquisitionMode.GetOnly)
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
                    if (mode == AcquisitionMode.GetOnly ||
                        mode == AcquisitionMode.GetOnlyIgnoreState)
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
                    if (mode == AcquisitionMode.CreateOnly)
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

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <param name="mode">Whether to retrieve an existing record, create one, or allow either operation.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public ValueTask<TWriter?> TryLockAsync(TKey key, AcquisitionMode mode = AcquisitionMode.GetOnly) => this.TryLockAsync(key, ValueLinkGlobal.LockTimeoutInMilliseconds, default, mode);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <param name="mode">Whether to retrieve an existing record, create one, or allow either operation.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, AcquisitionMode mode = AcquisitionMode.GetOnly) => this.TryLockAsync(key, millisecondsTimeout, default, mode);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="key">The unique key of the record.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
    /// <param name="mode">Whether to retrieve an existing record, create one, or allow either operation.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public async ValueTask<TWriter?> TryLockAsync(TKey key, int millisecondsTimeout, CancellationToken cancellationToken, AcquisitionMode mode = AcquisitionMode.GetOnly)
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
                    if (mode == AcquisitionMode.GetOnly ||
                        mode == AcquisitionMode.GetOnlyIgnoreState)
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
                    if (mode == AcquisitionMode.CreateOnly)
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

            bool entered;
            try
            {
                entered = await x.WriterSemaphoreInternal.EnterAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                throw;
            }

            if (entered)
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

    /// <summary>
    /// Waits for a writer for a selected record, returning null when acquisition is unavailable.
    /// </summary>
    /// <param name="predicate">A selector evaluated under the owner lock; return only an object owned by this owner.</param>
    /// <returns>A writer to dispose after use, or null if acquisition is unavailable.</returns>
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

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="predicate">A selector evaluated under the owner lock; return only an object owned by this owner.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate) => this.TryLockAsync(predicate, ValueLinkGlobal.LockTimeoutInMilliseconds, default);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="predicate">A selector evaluated under the owner lock; return only an object owned by this owner.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
    public ValueTask<TWriter?> TryLockAsync(Func<RepeatableReadGoshujin<TKey, TObject, TGoshujin, TWriter>, TObject?> predicate, int millisecondsTimeout) => this.TryLockAsync(predicate, millisecondsTimeout, default);

    /// <summary>
    /// Asynchronously acquires a writer, returning null on timeout or unavailable acquisition. Cancellation while waiting propagates to the caller.
    /// </summary>
    /// <param name="predicate">A selector evaluated under the owner lock; return only an object owned by this owner.</param>
    /// <param name="millisecondsTimeout">The writer-wait timeout in milliseconds, or -1 to wait indefinitely.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
    /// <returns>A task yielding a disposable writer, or null on timeout or unavailable acquisition.</returns>
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

            bool entered;
            try
            {
                entered = await x.WriterSemaphoreInternal.EnterAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                ((IRepeatableReadSemaphore)this).LockAndRelease(ref count);
                throw;
            }

            if (entered)
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
