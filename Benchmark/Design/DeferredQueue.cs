// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace ValueLink;

/// <summary>
/// Represents a queue that allows deferred adding and removing objects of type <typeparamref name="TObject"/> to/from a <typeparamref name="TGoshujin"/>.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the objects managed by the goshujin.</typeparam>
public ref struct DeferredQueue<TGoshujin, TObject>
    where TGoshujin : IGoshujin<TObject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeferredQueue{TGoshujin, TObject}"/> struct.
    /// </summary>
    /// <param name="goshujin">The goshujin instance.</param>
    public DeferredQueue(TGoshujin goshujin)
    {
        this.Goshujin = goshujin;
    }

    [Obsolete("Use DeferredQueue(TGoshujin goshujin) to create a new instance of DeferredQueue", true)]
    public DeferredQueue()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets the goshujin.
    /// </summary>
    public readonly TGoshujin Goshujin;

    private Queue<TObject>? queue;

    /// <summary>
    /// Gets the number of objects in the queue.
    /// </summary>
    public int Count => this.queue?.Count ?? 0;

    /// <summary>
    /// Adds an object to the queue.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    public void Add(TObject obj)
    {
        this.queue ??= new();
        this.queue.Enqueue(obj);
    }

    /// <summary>
    /// Adds all queued objects to the goshujin.
    /// </summary>
    public void DeferredAdd()
    {
        if (this.queue is not null)
        {
            while (this.queue.TryDequeue(out var x))
            {
                this.Goshujin.Add(x);
            }

            this.queue = default;
        }
    }

    /// <summary>
    /// Removes all queued objects from the goshujin.
    /// </summary>
    public void DeferredRemove()
    {
        if (this.queue is not null)
        {
            while (this.queue.TryDequeue(out var x))
            {
                this.Goshujin.Remove(x);
            }

            this.queue = default;
        }
    }
}
