// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using ValueLink;

namespace Arc.Collections;

/// <summary>
/// Buffers up to four object references inline before allocating an overflow list.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the objects.</typeparam>
/// <remarks>
/// Used by collection experiments to defer mutations until enumeration ends.
/// </remarks>
public ref struct TemporaryObjectsObsolete<TGoshujin, TObject>
    where TGoshujin : class, IGoshujin
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
{
    private const int StackSize = 4;

    private TObject? obj0;
    private TObject? obj1;
    private TObject? obj2;
    private TObject? obj3;
    private List<TObject>? list;

    /// <summary>
    /// Gets the number of objects in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            if (this.obj0 is null)
            {
                return 0;
            }
            else if (this.obj1 is null)
            {
                return 1;
            }
            else if (this.obj2 is null)
            {
                return 2;
            }
            else if (this.obj3 is null)
            {
                return 3;
            }
            else
            {
                if (this.list is null)
                {
                    return StackSize;
                }
                else
                {
                    return StackSize + this.list.Count;
                }
            }
        }
    }

    /// <summary>
    /// Adds an object to the end of the queue.
    /// </summary>
    /// <param name="obj">The object to add to the queue.</param>
    public void Add(TObject obj)
    {
        if (this.obj0 is null)
        {
            this.obj0 = obj;
            return;
        }

        if (this.obj1 is null)
        {
            this.obj1 = obj;
            return;
        }

        if (this.obj2 is null)
        {
            this.obj2 = obj;
            return;
        }

        if (this.obj3 is null)
        {
            this.obj3 = obj;
            return;
        }

        this.list ??= new();
        this.list.Add(obj);
    }

    public void AddToGoshujin(TGoshujin goshujin)
    {
        if (this.obj0 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj0, goshujin);
        if (this.obj1 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj1, goshujin);
        if (this.obj2 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj2, goshujin);
        if (this.obj3 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj3, goshujin);
        if (this.list is null)
        {
            return;
        }

        foreach (var x in this.list)
        {
            TObject.SetGoshujin(x, goshujin);
        }
    }

    public void RemoveFromGoshujin()
    {
        if (this.obj0 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj0, default);
        if (this.obj1 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj1, default);
        if (this.obj2 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj2, default);
        if (this.obj3 is null)
        {
            return;
        }

        TObject.SetGoshujin(this.obj3, default);
        if (this.list is null)
        {
            return;
        }

        foreach (var x in this.list)
        {
            TObject.SetGoshujin(x, default);
        }
    }
}
