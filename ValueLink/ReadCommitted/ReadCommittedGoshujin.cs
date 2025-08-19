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
    where TGoshujin : class, IGoshujin<TObject>
{
    public abstract Lock LockObject { get; }

    protected abstract TObject? FindFirst(TKey key);

    protected abstract TObject NewObject(TKey key);

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

    /*public ValueTask<TData?> GetOrCreate(TKey key)
    {
        TObject? obj;
        using (this.LockObject.EnterScope())
        {
            obj = this.FindFirst(key) ?? this.NewObject(key);
        }

        return obj.TryGet();
    }*/

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

    public TObject[] GetArray()
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        return array;
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
        using (this.LockObject.EnterScope())
        {
            var g = this as IGoshujin;
            g?.ClearInternal();

            var e = (this as IEnumerable<TObject>) ?? [];
            foreach (var x in e)
            {
                if (x is IStructualObject y)
                {
                    y.Delete();
                }
            }
        }
    }
}
