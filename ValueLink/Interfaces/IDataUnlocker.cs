// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Defines an interface for releasing a lock on a data resource.
/// </summary>
public interface IDataUnlocker
{
    /// <summary>
    /// Releases the lock on the data resource.
    /// </summary>
    void Unlock();

    /// <summary>
    /// Releases the lock on the data resource and deletes it.
    /// </summary>
    /// <returns>Returns <see langword="true" /> if the deletion succeeds, or <see langword="false" /> if it has already been deleted.</returns>
    bool UnlockAndDelete();
}
