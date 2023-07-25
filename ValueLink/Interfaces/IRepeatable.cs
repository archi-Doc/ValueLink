// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ValueLink;

/// <summary>
/// Specify the behavior of TryLock function to either retrieve an existing object or create a new one if it does not exist.
/// </summary>
public enum TryLockMode
{
    /// <summary>
    /// Retrieves the object that matches the specified key and attempts to lock it.<br/>
    /// If it does not exist, it returns null.
    /// </summary>
    Get,

    /// <summary>
    /// Creates an object with the specified key and attempts to lock it.<br/>
    /// If it already exists, it returns null.
    /// </summary>
    Create,

    /// <summary>
    /// Retrieves the object that matches the specified key, or creates it if it does not exist, and attempts to lock it.
    /// </summary>
    GetOrCreate,
}

/// <summary>
/// A base interface for Repeatable reads.
/// </summary>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public interface IRepeatableObject<TGoshujin, TWriter>
    where TGoshujin : class
    where TWriter : class
{
    TWriter? TryLock();

    void AddToGoshujinInternal(TGoshujin g);
}

/// <summary>
/// A base interface for Repeatable reads.
/// </summary>
/// <typeparam name="TKey">The type of key class.</typeparam>
/// <typeparam name="TObject">The type of object class.</typeparam>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
/// <typeparam name="TWriter">The type of writer class.</typeparam>
public interface IRepeatableGoshujin<TKey, TObject, TGoshujin, TWriter>
    where TObject : class, IRepeatableObject<TGoshujin, TWriter>
    where TGoshujin : class
    where TWriter : class
    // where TGoshujin : IRepeatableGoshujin<TKey, TObject, TWriter, TGoshujin>
{
    public object SyncObject { get; }

    protected TObject? FindFirst(TKey key);

    protected TObject NewObject(TKey key);

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

            if (x.TryLock() is { } writer)
            {
                return writer;
            }
        }
    }
}
