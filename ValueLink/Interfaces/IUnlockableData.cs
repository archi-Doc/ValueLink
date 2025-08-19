// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Defines an interface for releasing a lock on a data resource.
/// </summary>
public interface IUnlockableData
{
    /// <summary>
    /// Gets or sets a value indicating whether the data resource is protected from deletion.
    /// </summary>
    bool IsProtected { get; internal set; }

    /// <summary>
    /// Releases the lock on the data resource.
    /// </summary>
    void Unlock();
}
