// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// A base interface for serializable.
/// </summary>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
public abstract class SerializableGoshujin<TObject, TGoshujin> : ISerializableSemaphore
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : SerializableGoshujin<TObject, TGoshujin>, IGoshujin<TObject>
{
    public abstract SemaphoreLock LockObject { get; }

    protected async Task<bool> GoshujinStoreData(StoreMode storeMode)
    {
        if (storeMode == StoreMode.StoreOnly)
        {
            TObject[] array;
            await this.LockObject.EnterAsync().ConfigureAwait(false);
            try
            {
                array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
            }
            finally
            {
                this.LockObject.Exit();
            }

            foreach (var x in array)
            {
                if (x is IStructualObject y && await y.StoreData(storeMode).ConfigureAwait(false) == false)
                {
                    return false;
                }
            }
        }
        else
        {
            await this.LockObject.EnterAsync().ConfigureAwait(false);
            try
            {
                var e = (this as IEnumerable<TObject>) ?? [];
                foreach (var x in e)
                {
                    if (x is IStructualObject y && await y.StoreData(storeMode).ConfigureAwait(false) == false)
                    {
                        return false;
                    }
                }
            }
            finally
            {
                this.LockObject.Exit();
            }
        }

        return true;
    }

    protected void GoshujinErase()
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
                    y.Erase();
                }
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
}
