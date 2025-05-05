// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    /// Gets a value indicating whether to remove the item if it is not found in the comparison goshujin.
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
    /// Validate the new item. If the method returns <see langword="true"/>, the item is added to the collection.<br/>
    /// If an existing item with the same unique key is found, it is referenced as oldItem.<br/>
    /// In such cases, returning <see langword="false"/> retains the oldItem; returning <see langword="true"/> replaces the oldItem with the newItem.
    /// </summary>
    /// <param name="goshujin">The goshujin instance.</param>
    /// <param name="newItem">The new item.</param>
    /// <param name="oldItem">The old item.</param>
    /// <returns><see langword="true"/> if the validation is successful; otherwise, <see langword="false"/>.</returns>
    bool Validate(object goshujin, object newItem, object? oldItem);
}
