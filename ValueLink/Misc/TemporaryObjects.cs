// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace ValueLink;

/// <summary>
/// A queue of temporary objects created using a ref struct.<br/>
/// If the count is 4 or fewer, it avoids creating a <see cref="List{T}"/> and keeps the objects on the stack.<br/>
/// It is primarily used when you need to manipulate a collection after exiting a for or foreach loop.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the objects.</typeparam>
public ref struct TemporaryObjects<TGoshujin, TObject>
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
