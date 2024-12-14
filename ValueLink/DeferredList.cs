// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace ValueLink;

#pragma warning disable SA1629
/// <summary>
/// Represents a list that allows deferred adding and removing objects of type <typeparamref name="TObject"/> to/from a <typeparamref name="TGoshujin"/>.<br/><br/>
/// var list = new DeferredList&lt;Goshujin, Object&gt;(goshujin);<br/>
/// list.Add(obj1);<br/>
/// list.Add(obj2);<br/>
/// list.DeferredRemove();
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the objects managed by the goshujin.</typeparam>
#pragma warning restore SA1629
public ref struct DeferredList<TGoshujin, TObject>
    where TObject : class
    where TGoshujin : IGoshujin<TObject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeferredList{TGoshujin, TObject}"/> struct.
    /// </summary>
    /// <param name="goshujin">The goshujin instance.</param>
    public DeferredList(TGoshujin goshujin)
    {
        this.Goshujin = goshujin;
    }

    [Obsolete("Use DeferredList(TGoshujin goshujin) to create a new instance of DeferredList", true)]
    public DeferredList()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets the goshujin.
    /// </summary>
    public readonly TGoshujin Goshujin;

    private TObject? obj0;
    private TObject? obj1;
    private TObject? obj2;
    private TObject? obj3;
    private List<TObject>? list;

    /// <summary>
    /// Gets the number of objects in the list.
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
                return 4 + this.list?.Count ?? 0;
            }
        }
    }

    /// <summary>
    /// Adds an object to the list.
    /// </summary>
    /// <param name="obj">The object to add.</param>
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

        this.list ??= new List<TObject>();
        this.list.Add(obj);
    }

    /// <summary>
    /// Adds all listed objects to the goshujin.
    /// </summary>
    public void DeferredAdd()
    {
        if (this.obj0 is null)
        {
            return;
        }

        this.Goshujin.Add(this.obj0);
        this.obj0 = default;
        if (this.obj1 is null)
        {
            return;
        }

        this.Goshujin.Add(this.obj1);
        this.obj1 = default;
        if (this.obj2 is null)
        {
            return;
        }

        this.Goshujin.Add(this.obj2);
        this.obj2 = default;
        if (this.obj3 is null)
        {
            return;
        }

        this.Goshujin.Add(this.obj3);
        this.obj3 = default;
        if (this.list is null)
        {
            return;
        }

        foreach (var x in this.list)
        {
            this.Goshujin.Add(x);
        }

        this.list.Clear();
    }

    /// <summary>
    /// Removes all listed objects from the goshujin.
    /// </summary>
    public void DeferredRemove()
    {
        if (this.obj0 is null)
        {
            return;
        }

        this.Goshujin.Remove(this.obj0);
        this.obj0 = default;
        if (this.obj1 is null)
        {
            return;
        }

        this.Goshujin.Remove(this.obj1);
        this.obj1 = default;
        if (this.obj2 is null)
        {
            return;
        }

        this.Goshujin.Remove(this.obj2);
        this.obj2 = default;
        if (this.obj3 is null)
        {
            return;
        }

        this.Goshujin.Remove(this.obj3);
        this.obj3 = default;
        if (this.list is null)
        {
            return;
        }

        foreach (var x in this.list)
        {
            this.Goshujin.Remove(x);
        }

        this.list.Clear();
    }

    /// <summary>
    /// Clears all objects from the list.
    /// </summary>
    public void Clear()
    {
        this.obj0 = default;
        this.obj1 = default;
        this.obj2 = default;
        this.obj3 = default;
        this.list?.Clear();
    }
}
