// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace ValueLink;

/*
/// <summary>
/// Helper functions for ValueLink.
/// </summary>
public static class ValueLinkHelper
{
    /// <summary>
    /// Adds all objects in the specified queue to the goshujin.<br/>
    /// To achieve the best performance, avoid virtualization and call the goshujin function within a manually written for loop.<br/><br/>
    /// foreach (var x in queue) { goshujin.Add(x); }
    /// </summary>
    /// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
    /// <typeparam name="TObject">The type of the objects in the queue.</typeparam>
    /// <param name="goshujin">The goshujin to which to add objects.</param>
    /// <param name="queue">The queue containing objects to be added.</param>
    public static void Add<TGoshujin, TObject>(this TGoshujin goshujin, ref TemporaryQueue<TObject> queue)

        where TGoshujin : IGoshujin<TObject>
        where TObject : class
    {
        foreach (var x in queue)
        {
            goshujin.Add(x);
        }
    }

    /// <summary>
    /// Removes all objects in the specified queue from the goshujin.<br/>
    /// To achieve the best performance, avoid virtualization and call the goshujin function within a manually written for loop.<br/><br/>
    /// foreach (var x in queue) { goshujin.Remove(x); }
    /// </summary>
    /// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
    /// <typeparam name="TObject">The type of the objects in the queue.</typeparam>
    /// <param name="goshujin">The goshujin from which to remove objects.</param>
    /// <param name="queue">The queue containing objects to be removed.</param>
    public static void Remove<TGoshujin, TObject>(this TGoshujin goshujin, ref TemporaryQueue<TObject> queue)
        where TGoshujin : IGoshujin<TObject>
        where TObject : class
    {
        foreach (var x in queue)
        {
            goshujin.Remove(x);
        }
    }
}*/
