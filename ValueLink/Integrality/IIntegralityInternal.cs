// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace ValueLink.Integrality;

/// <summary>
/// Represents the internal interface for integrality.
/// </summary>
public interface IIntegralityInternal
{
    /// <summary>
    /// Gets the maximum number of items.
    /// </summary>
    int MaxItems { get; }

    /// <summary>
    /// Gets a value indicating whether to remove the item if it is not found.
    /// </summary>
    bool RemoveIfItemNotFound { get; }

    /// <summary>
    /// Gets the maximum length of the memory.
    /// </summary>
    int MaxMemoryLength { get; }

    /// <summary>
    /// Gets the maximum integration count.
    /// </summary>
    int MaxIntegrationCount { get; }

    /// <summary>
    /// Gets the target hash.
    /// </summary>
    public ulong TargetHash { get; }

    /// <summary>
    /// Gets the key hash cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="clear">A value indicating whether to clear the cache.</param>
    /// <returns>The key hash cache.</returns>
    Dictionary<TKey, ulong> GetKeyHashCache<TKey>(bool clear)
        where TKey : struct;

    /// <summary>
    /// Validates the specified objects.
    /// </summary>
    /// <param name="goshujin">The goshujin.</param>
    /// <param name="newItem">The new item.</param>
    /// <param name="oldItem">The old item.</param>
    /// <returns><c>true</c> if the validation is successful; otherwise, <c>false</c>.</returns>
    bool Validate(object goshujin, object newItem, object? oldItem);
}
