// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public GoshujinState State { get; set; }

    protected Task<bool> GoshujinSave(UnloadMode unloadMode)
    {
        TObject[] array;
        using (this.LockObject.EnterScope())
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return Task.FromResult(true);
            }
            else if (unloadMode != UnloadMode.NoUnload)
            {// TryUnload or ForceUnload
                ((IRepeatableSemaphore)this).SetReleasing();
                /*if (unloadMode == UnloadMode.TryUnload && this.SemaphoreCount > 0)
                {// Acquired.
                    return Task.FromResult(false);
                }*/
            }

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();

            foreach (var x in array)
            {
                if (x is IStructualObject y && y.Save(unloadMode).Result == false)
                {
                    return Task.FromResult(false);
                }
            }

            if (unloadMode != UnloadMode.NoUnload)
            {// Unloaded
                ((IRepeatableSemaphore)this).SetObsolete();
            }
        }

        return Task.FromResult(true);
    }

    protected async Task<bool> GoshujinStoreData(StoreMode storeMode)
    {
        TObject[] array;

        await this.LockObject.EnterAsync().ConfigureAwait(false);
        try
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return true;
            }
            else if (storeMode == StoreMode.Release)
            {
                ((IRepeatableSemaphore)this).SetReleasing();//
                /*if (unloadMode == UnloadMode.TryUnload && this.SemaphoreCount > 0)
                {// Acquired.
                    return Task.FromResult(false);
                }*/
            }

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();

            foreach (var x in array)
            {
                if (x is IStructualObject y && y.StoreData(storeMode).Result == false)
                {
                    return false;
                }
            }

            if (storeMode == StoreMode.Release)
            {// Released
                ((IRepeatableSemaphore)this).SetObsolete();
            }
        }
        finally
        {
            this.LockObject.Exit();
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

            foreach (var x in array)
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
