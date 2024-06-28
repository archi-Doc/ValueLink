// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace ValueLink;

/// <summary>
/// Represents a list that allows deferred adding and removing objects of type <typeparamref name="TObject"/> to/from a <typeparamref name="TGoshujin"/>.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the objects managed by the goshujin.</typeparam>
public ref struct DeferredList<TGoshujin, TObject>
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

    /// <summary>
    /// Gets the goshujin.
    /// </summary>
    public readonly TGoshujin Goshujin;

    private List<TObject>? list;

    /// <summary>
    /// Gets the number of objects in the list.
    /// </summary>
    public int Count => this.list?.Count ?? 0;

    /// <summary>
    /// Adds an object to the list.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    public void Add(TObject obj)
    {
        this.list ??= new List<TObject>();
        this.list.Add(obj);
    }

    /// <summary>
    /// Adds all listed objects to the goshujin.
    /// </summary>
    public void DeferredAdd()
    {
        if (this.list is not null)
        {
            foreach (var x in this.list)
            {
                this.Goshujin.Add(x);
            }

            this.list = default;
        }
    }

    /// <summary>
    /// Removes all listed objects from the goshujin.
    /// </summary>
    public void DeferredRemove()
    {
        if (this.list is not null)
        {
            foreach (var x in this.list)
            {
                this.Goshujin.Remove(x);
            }

            this.list = default;
        }
    }
}
