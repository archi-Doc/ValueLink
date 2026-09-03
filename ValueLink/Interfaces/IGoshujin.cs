// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;

namespace ValueLink;

/// <summary>
/// Defines ownership and chain-management operations for linked objects.
/// </summary>
public interface IGoshujin
{
    /// <summary>
    /// Clears all chain memberships while preserving owner references. Acquire the owner lock when synchronization is required.
    /// </summary>
    void ClearChains();

    /// <summary>
    /// Removes primary-chain objects from every chain and resets their owner references. Acquire the owner lock when required.
    /// </summary>
    /// <remarks>
    /// Generated owners support this only with a primary chain and None or Serializable isolation;
    /// other configurations throw NotImplementedException.
    /// </remarks>
    void ClearAll();

    /// <summary>
    /// Gets an <see cref="IEnumerable"/> that iterates through the objects managed by the Goshujin.
    /// </summary>
    /// <returns>An <see cref="IEnumerable"/> for the objects in the Goshujin.</returns>
    IEnumerable GetEnumerableInternal();
}

/// <summary>
/// Defines ownership and chain-management operations for linked objects.
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

    /*/// <summary>
    /// Adds all objects in the specified queue to the Goshujin.
    /// </summary>
    /// <param name="queue">The queue containing objects to add.</param>
    void AddAll(ref TemporaryQueue<TObject> queue);

    /// <summary>
    /// Removes all objects in the specified queue from the Goshujin.
    /// </summary>
    /// <param name="queue">The queue containing objects to remove.</param>
    void RemoveAll(ref TemporaryQueue<TObject> queue);*/
}
