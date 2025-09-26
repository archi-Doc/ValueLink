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
    /// Releases the lock on the data resource and deletes it, optionally forcing deletion after the specified date and time.
    /// </summary>
    /// <param name="forceDeleteAfter">
    /// The time after which the deletion will be forced even if the object is protected.<br/>
    /// If <see langword="default"/>, waits indefinitely.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
    Task UnlockAndDelete(DateTime forceDeleteAfter = default);
}
