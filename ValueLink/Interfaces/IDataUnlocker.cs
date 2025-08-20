// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Defines an interface for releasing a lock on a data resource.
/// </summary>
public interface IDataUnlocker : IDataProtectionCounter
{
    /// <summary>
    /// Releases the lock on the data resource.
    /// </summary>
    void Unlock();
}
