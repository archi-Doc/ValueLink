// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace ValueLink;

/// <summary>
/// A base interface for serializable.
/// </summary>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
public abstract class SerializableGoshujin<TObject, TGoshujin> : IGoshujinSemaphore
    where TObject : class, IValueLinkObjectInternal<TGoshujin>
    where TGoshujin : SerializableGoshujin<TObject, TGoshujin>
{
    public abstract object SyncObject { get; }

    public GoshujinState State { get; set; }

    public int SemaphoreCount { get; set; }

    protected Task<bool> GoshujinSave(UnloadMode unloadMode)
    {
        TObject[] array;
        lock (this.SyncObject)
        {
            if (this.State == GoshujinState.Obsolete)
            {// Unloaded or deleted.
                return Task.FromResult(true);
            }
            else if (unloadMode != UnloadMode.NoUnload)
            {// TryUnload or ForceUnload
                ((IGoshujinSemaphore)this).SetUnloading();
                if (unloadMode == UnloadMode.TryUnload && this.SemaphoreCount > 0)
                {// Acquired.
                    return Task.FromResult(false);
                }
            }

            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();

            foreach (var x in array)
            {
                if (x is ITreeObject y && y.Save(unloadMode).Result == false)
                {
                    return Task.FromResult(false);
                }
            }

            if (unloadMode != UnloadMode.NoUnload)
            {// Unloaded
                ((IGoshujinSemaphore)this).SetObsolete();
            }
        }

        return Task.FromResult(true);
    }

    protected void GoshujinErase()
    {
        TObject[] array;
        lock (this.SyncObject)
        {
            ((IGoshujinSemaphore)this).SetObsolete();
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();

            foreach (var x in array)
            {
                if (x is ITreeObject y)
                {
                    y.Erase();
                }
            }
        }
    }

    public TObject[] GetArray()
    {
        TObject[] array;
        lock (this.SyncObject)
        {
            array = (this is IEnumerable<TObject> e) ? e.ToArray() : Array.Empty<TObject>();
        }

        return array;
    }
}
