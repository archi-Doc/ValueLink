// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Defines an interface for releasing a lock on a data resource.
/// </summary>
public interface IDataUnlocker
{
    /// <summary>
    /// Marks the data resource for deletion after the lock is released.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the data resource will be deleted after unlocking; otherwise, <c>false</c>.
    /// </returns>
    bool DeleteAfterUnlock();

    /// <summary>
    /// Releases the lock on the data resource.
    /// </summary>
    void Unlock();
}
