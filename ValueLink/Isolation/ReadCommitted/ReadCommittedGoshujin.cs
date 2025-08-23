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
/// Provides a base class for managing objects with read-committed isolation and mutual exclusion.
/// Supports object retrieval, creation, locking, deletion, and enumeration with thread safety.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify objects.</typeparam>
/// <typeparam name="TData">The type of the data managed by the objects. Must be non-null.</typeparam>
/// <typeparam name="TObject">
/// The type of the object managed by the goshujin. Must implement <see cref="IValueLinkObjectInternal{TGoshujin, TObject}"/> and <see cref="IDataLocker{TData}"/>.
/// </typeparam>
/// <typeparam name="TGoshujin">
/// The type of the goshujin (owner class). Must inherit from <see cref="ReadCommittedGoshujin{TKey, TData, TObject, TGoshujin}"/> and implement <see cref="IGoshujin"/>.
/// </typeparam>
public abstract class ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin> : IReadCommittedSemaphore
    where TData : notnull
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>, IDataLocker<TData>
    where TGoshujin : ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin>, IGoshujin
{
    /*/// <summary>
    /// The value used to mark an object as deleted in the protection counter.
    /// </summary>
    private const int DeletedCount = int.MinValue / 2;*/

    /// <summary>
    /// The delay in milliseconds used when waiting to retry deletion of a protected object.
    /// </summary>
    private const int DelayInMilliseconds = 10;

    /// <summary>
    /// Gets the lock object used for mutual exclusion.
    /// </summary>
    public abstract Lock LockObject { get; }

    /// <summary>
    /// Finds an object by its key.
    /// </summary>
    /// <param name="key">The key of the object to find.</param>
    /// <returns>The object if found; otherwise, <c>null</c>.</returns>
    protected abstract TObject? FindObject(TKey key);

    /// <summary>
    /// Creates a new object for the specified key.
    /// </summary>
    /// <param name="key">The key for which to create the object.</param>
    /// <returns>The newly created object.</returns>
    protected abstract TObject NewObject(TKey key);

    /// <summary>
    /// Gets the current state of the goshujin.
    /// </summary>
    public GoshujinState State { get; private set; } // Lock:LockObject

    /// <summary>
    /// Gets a value indicating whether the goshujin is in a valid state.
    /// </summary>
    public bool IsValid => this.State == GoshujinState.Valid;

    /// <summary>
    /// Sets the state of the goshujin to obsolete.
    /// </summary>
    public void SetObsolete()
        => this.State = GoshujinState.Obsolete;

    /// <summary>
    /// Executes the specified <paramref name="action"/> for every owned object.
    /// The specified delegate is invoked within mutual exclusion (inside a lock).
    /// </summary>
    /// <param name="action">The delegate to execute per item.</param>
    public void ForEach(Action<TObject> action)
    {
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return;
            }

            if (((IGoshujin)this).GetEnumerableInternal() is IEnumerable<TObject> enumerable)
            {
                foreach (var x in enumerable)
                {
                    action(x);
                }
            }
        }
    }

    /// <summary>
    /// Finds the first object matching the specified key.
    /// </summary>
    /// <param name="key">The key of the object to find.</param>
    /// <param name="lockMode">The lock mode specifying get, create, or get-or-create behavior.</param>
    /// <returns>The object if found; otherwise, <c>null</c>.</returns>
    public TObject? FindFirst(TKey key, LockMode lockMode = LockMode.Get)
    {
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return default;
            }

            var obj = this.FindObject(key);
            if (obj is null)
            {// Object not found
                if (lockMode == LockMode.Get)
                {// Get
                    return default;
                }
                else
                {// Create or GetOrCreate
                    obj = this.NewObject(key);
                    TObject.AddToGoshujin(obj, (TGoshujin)this, true);
                }
            }
            else
            {// Object found
                if (lockMode == LockMode.Create)
                {// Create
                    return default;
                }
            }

            return obj;
        }
    }

    /// <summary>
    /// Attempts to retrieve the data for the object matching the specified key, without acquiring a lock.
    /// </summary>
    /// <param name="key">The key of the object to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{TData}"/> containing the data if available; otherwise, <c>null</c>.
    /// </returns>
    public ValueTask<TData?> TryGet(TKey key, CancellationToken cancellationToken = default)
        => this.TryGet(key, ValueLinkGlobal.LockTimeout, cancellationToken);

    /// <summary>
    /// Attempts to retrieve the data for the object matching the specified key, without acquiring a lock.
    /// </summary>
    /// <param name="key">The key of the object to retrieve.</param>
    /// <param name="timeout">The maximum time to wait for the lock. If <see cref="TimeSpan.Zero"/>, the method returns immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{TData}"/> containing the data if available; otherwise, <c>null</c>.
    /// </returns>
    public ValueTask<TData?> TryGet(TKey key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return ValueTask.FromResult<TData?>(default);
            }

            obj = this.FindObject(key);
            if (obj is null)
            {
                return ValueTask.FromResult<TData?>(default);
            }
        }

        return obj.TryGet(timeout, cancellationToken);
    }

    /// <summary>
    /// Attempts to acquire a lock on the object matching the specified key, with the specified lock mode.
    /// </summary>
    /// <param name="key">The key of the object to lock.</param>
    /// <param name="lockMode">The lock mode specifying get, create, or get-or-create behavior.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{DataScope}"/> containing the result and the locked data if successful.
    /// </returns>
    public ValueTask<DataScope<TData>> TryLock(TKey key, LockMode lockMode, CancellationToken cancellationToken = default)
        => this.TryLock(key, lockMode, ValueLinkGlobal.LockTimeout, cancellationToken);

    /// <summary>
    /// Attempts to acquire a lock on the object matching the specified key, with the specified lock mode.
    /// </summary>
    /// <param name="key">The key of the object to lock.</param>
    /// <param name="lockMode">The lock mode specifying get, create, or get-or-create behavior.</param>
    /// <param name="timeout">The maximum time to wait for the lock. If <see cref="TimeSpan.Zero"/>, the method returns immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{DataScope}"/> containing the result and the locked data if successful.
    /// </returns>
    public ValueTask<DataScope<TData>> TryLock(TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return ValueTask.FromResult(new DataScope<TData>(DataScopeResult.Obsolete));
            }

            obj = this.FindObject(key);
            if (obj is null)
            {// Object not found
                if (lockMode == LockMode.Get)
                {// Get
                    return ValueTask.FromResult(new DataScope<TData>(DataScopeResult.NotFound));
                }
                else
                {// Create or GetOrCreate
                    obj = this.NewObject(key);
                    TObject.AddToGoshujin(obj, (TGoshujin)this, true);
                }
            }
            else
            {// Object found
                if (lockMode == LockMode.Create)
                {// Create
                    return ValueTask.FromResult(new DataScope<TData>(DataScopeResult.AlreadyExists));
                }
            }
        }

        return obj.TryLock(timeout, cancellationToken);
    }

    /// <summary>
    /// Attempts to delete the object matching the specified key.
    /// If the object is protected, waits until it can be deleted or until <paramref name="forceDeleteAfter"/> is reached.
    /// </summary>
    /// <param name="key">The key of the object to delete.</param>
    /// <param name="forceDeleteAfter">
    /// The time after which the deletion will be forced even if the object is protected.
    /// If <see cref="DateTime.MinValue"/>, waits indefinitely.
    /// </param>
    /// <returns>
    /// A <see cref="Task{DataScopeResult}"/> indicating the result of the deletion attempt.
    /// </returns>
    public async Task<DataScopeResult> TryDelete(TKey key, DateTime forceDeleteAfter = default)
    {
        TObject? obj;
        var delay = false;

Retry:
        if (delay)
        {
            await Task.Delay(DelayInMilliseconds);
        }

        using (this.LockObject.EnterScope())
        {
            obj = this.FindObject(key);
            if (obj is null)
            {// Object not found
                return DataScopeResult.NotFound;
            }

            // Unprotected -> Deleted
            if (Interlocked.CompareExchange(ref obj.GetProtectionStateRef(), ObjectProtectionState.Deleted, ObjectProtectionState.Unprotected) != ObjectProtectionState.Protected)
            {// Successfully marked as deleted (Unprotected->Deleted or Deleted->Deleted)
            }
            else
            {// Protected
                if (forceDeleteAfter == default ||
                        DateTime.UtcNow <= forceDeleteAfter)
                {// Wait for a specified time, then attempt deletion again.
                    delay = true;
                    goto Retry;
                }
                else
                {// Force delete
                    obj.GetProtectionStateRef() = ObjectProtectionState.Deleted;
                }
            }

            TObject.RemoveFromGoshujin(obj, (TGoshujin)this, true, true);
        }

        if (obj is IStructualObject y)
        {
            await y.Delete(forceDeleteAfter).ConfigureAwait(false);
        }

        return DataScopeResult.Success;
    }

    /// <summary>
    /// Gets an array of all objects managed by the goshujin.
    /// </summary>
    /// <returns>
    /// An array of objects; empty if the goshujin is not valid or contains no objects.
    /// </returns>
    public TObject[] GetArray()
    {
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return [];
            }

            if (((IGoshujin)this).GetEnumerableInternal() is IEnumerable<TObject> enumerable)
            {
                return enumerable.ToArray();
            }
            else
            {
                return [];
            }
        }
    }

    /// <summary>
    /// Stores the data for all objects managed by the goshujin using the specified store mode.
    /// </summary>
    /// <param name="storeMode">The mode specifying how data should be stored.</param>
    /// <returns>
    /// A <see cref="Task{Boolean}"/> indicating <c>true</c> if all data was stored successfully; otherwise, <c>false</c>.
    /// </returns>
    protected async Task<bool> GoshujinStoreData(StoreMode storeMode)
    {
        var array = this.GetArray();
        foreach (var x in array)
        {
            if (x is IStructualObject y && await y.StoreData(storeMode).ConfigureAwait(false) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Deletes all objects managed by the goshujin, waiting for protection to be released or until <paramref name="forceDeleteAfter"/> is reached.
    /// </summary>
    /// <param name="forceDeleteAfter">
    /// The time after which deletion will be forced even if objects are protected.
    /// If <see cref="DateTime.MinValue"/>, waits indefinitely.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task GoshujinDelete(DateTime forceDeleteAfter)
    {
        TObject[] array = [];
        using (this.LockObject.EnterScope())
        {
            this.SetObsolete();

            if (this is IGoshujin goshujin)
            {
                if (goshujin.GetEnumerableInternal() is IEnumerable<TObject> enumerable)
                {
                    array = enumerable.ToArray();
                }

                goshujin.ClearInternal();
            }
        }

        foreach (var obj in array)
        {
Retry: // Unprotected -> Deleted
            if (Interlocked.CompareExchange(ref obj.GetProtectionStateRef(), ObjectProtectionState.Deleted, ObjectProtectionState.Unprotected) != ObjectProtectionState.Protected)
            {// Successfully marked as deleted (Unprotected->Deleted or Deleted->Deleted)
            }
            else
            {// Protected
                if (forceDeleteAfter == default ||
                    DateTime.UtcNow <= forceDeleteAfter)
                {// Wait for a specified time, then attempt deletion again.
                    await Task.Delay(DelayInMilliseconds);
                    goto Retry;
                }
                else
                {// Force delete
                    obj.GetProtectionStateRef() = ObjectProtectionState.Deleted;
                }
            }

            if (obj is IStructualObject y)
            {
                await y.Delete(forceDeleteAfter).ConfigureAwait(false);
            }
        }
    }
}
