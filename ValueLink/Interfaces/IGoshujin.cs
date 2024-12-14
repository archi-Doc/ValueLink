// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using Arc.Collections;

namespace ValueLink;

/// <summary>
/// A base interface for Goshujin (Owner class).
/// </summary>
public interface IGoshujin
{
}

/// <summary>
/// A base interface for Goshujin (Owner class).
/// </summary>
/// <typeparam name="TObject">The type of the object to be managed by the Goshujin.</typeparam>
public interface IGoshujin<TObject> : IGoshujin
    where TObject : class
{
    /// <summary>
    /// Add an object to the Goshujin.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    void Add(TObject obj);

    /// <summary>
    /// Remove an object from the Goshujin.
    /// </summary>
    /// <param name="obj">The object to remove.</param>
    /// <returns>True if the object is removed successfully; otherwise, false.</returns>
    bool Remove(TObject obj);

    void AddAll(ref TemporaryQueue<TObject> queue);

    void RemoveAll(ref TemporaryQueue<TObject> queue);
}
