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
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>, ILockableData<TData>
    where TGoshujin : ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin>, IGoshujin
{
    public abstract Lock LockObject { get; }

    protected abstract TObject? FindFirst(TKey key);

    protected abstract TObject NewObject(TKey key);

    /// <summary>
    /// Executes the specified <paramref name="action"/> for every owned object.<br/>
    /// The specified delegate is invoked within mutual exclusion (inside a lock).
    /// </summary>
    /// <param name="action">The delegate to execute per item.</param>
    public void ForEach(Action<TObject> action)
    {
        using (this.LockObject.EnterScope())
        {
            if (((IGoshujin)this).GetEnumerableInternal() is IEnumerable<TObject> enumerable)
            {
                foreach (var x in enumerable)
                {
                    action(x);
                }
            }
        }
    }

    public ValueTask<TData?> TryGet(TKey key, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
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
            obj = this.FindFirst(key);
            if (obj is null)
            {// Object not found
                if (lockMode == LockMode.Get)
                {// Get
                    return ValueTask.FromResult(new DataScope<TData>(DataLockResult.NotFound));
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
                    return ValueTask.FromResult(new DataScope<TData>(DataLockResult.AlreadyExists));
                }
            }
        }

        return obj.TryLock(ValueLinkGlobal.LockTimeout, cancellationToken);
    }

    public DataLockResult Delete(TKey key, CancellationToken cancellationToken = default)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            obj = this.FindFirst(key);
            if (obj is null)
            {// Object not found
                return DataLockResult.NotFound;
            }

            TObject.RemoveFromGoshujin(obj, (TGoshujin)this, true, true);
        }

        if (obj is IStructualObject y)
        {
            y.Delete();
        }

        return DataLockResult.Success;
    }

    public TObject[] GetArray()
    {
        using (this.LockObject.EnterScope())
        {
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

    protected void GoshujinDelete()
    {
        TObject[] array = [];
        using (this.LockObject.EnterScope())
        {
            if (this is IGoshujin goshujin)
            {
                if (goshujin.GetEnumerableInternal() is IEnumerable<TObject> enumerable)
                {
                    array = enumerable.ToArray();
                }

                goshujin.ClearInternal();
            }
        }

        foreach (var x in array)
        {
            if (x is IStructualObject y)
            {
                y.Delete();
            }
        }
    }
}
