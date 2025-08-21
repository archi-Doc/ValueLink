// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

public abstract class ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin> : IReadCommittedSemaphore
    where TData : notnull
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>, IDataLocker<TData>
    where TGoshujin : ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin>, IGoshujin
{
    private const int DeletedCount = int.MinValue / 2;
    private const int DelayInMilliseconds = 10;

    public abstract Lock LockObject { get; }

    protected abstract TObject? FindFirst(TKey key);

    protected abstract TObject NewObject(TKey key);

    public GoshujinState State { get; private set; } // Lock:LockObject

    public bool IsValid => this.State == GoshujinState.Valid;

    public void SetObsolete()
        => this.State = GoshujinState.Obsolete;

    /// <summary>
    /// Executes the specified <paramref name="action"/> for every owned object.<br/>
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

    /*public TData? FindFirst(TKey key)
    {
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return default;
            }

            return this.FindFirst(key);
        }
    }*/

    public ValueTask<TData?> TryGet(TKey key, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return ValueTask.FromResult<TData?>(default);
            }

            obj = this.FindFirst(key);
            if (obj is null)
            {
                return ValueTask.FromResult<TData?>(default);
            }
        }

        return obj.TryGet(ValueLinkGlobal.LockTimeout, cancellationToken);
    }

    public ValueTask<DataScope<TData>> TryLock(TKey key, LockMode lockMode, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {
                return ValueTask.FromResult(new DataScope<TData>(DataScopeResult.Obsolete));
            }

            obj = this.FindFirst(key);
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

            // Increments the ProtectionCount.
            // Since this is within a lock scope, deletion will not occur.
            Interlocked.Increment(ref obj.GetProtectionCounterRef());
        }

        return obj.TryLock(ValueLinkGlobal.LockTimeout, cancellationToken);
    }

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
            obj = this.FindFirst(key);
            if (obj is null)
            {// Object not found
                return DataScopeResult.NotFound;
            }

            int current;
            do
            {
                current = Volatile.Read(ref obj.GetProtectionCounterRef());
                if (current > 0)
                {// Protected
                    if (forceDeleteAfter == default ||
                        DateTime.UtcNow <= forceDeleteAfter)
                    {// Wait for a specified time, then attempt deletion again.
                        delay = true;
                        goto Retry;
                    }
                    else
                    {// Force delete
                        break;
                    }
                }
            }
            while (Interlocked.CompareExchange(ref obj.GetProtectionCounterRef(), DeletedCount, current) != current);

            TObject.RemoveFromGoshujin(obj, (TGoshujin)this, true, true);
        }

        if (obj is IStructualObject y)
        {
            await y.Delete(forceDeleteAfter).ConfigureAwait(false);
        }

        return DataScopeResult.Success;
    }

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
            int current;
            do
            {
Retry:
                current = Volatile.Read(ref obj.GetProtectionCounterRef());
                if (current > 0)
                {// Protected
                    if (forceDeleteAfter == default ||
                        DateTime.UtcNow <= forceDeleteAfter)
                    {// Wait for a specified time, then attempt deletion again.
                        await Task.Delay(DelayInMilliseconds);
                        goto Retry;
                    }
                    else
                    {// Force delete
                        break;
                    }
                }
            }
            while (Interlocked.CompareExchange(ref obj.GetProtectionCounterRef(), DeletedCount, current) != current);

            if (obj is IStructualObject y)
            {
                await y.Delete(forceDeleteAfter).ConfigureAwait(false);
            }
        }
    }
}
