// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Defines members for releasing and managing the validity of a lock on a data resource.
/// </summary>
public interface IDataUnlocker
{
    /// <summary>
    /// Releases the lock on the associated data resource.
    /// </summary>
    void Unlock();

    /// <summary>
    /// Releases the lock and attempts to delete the associated data resource.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the resource was deleted by this call;
    /// otherwise, <see langword="false" /> if it had already been deleted.
    /// </returns>
    bool UnlockAndDelete();

    /// <summary>
    /// Marks tthe associated data resource as valid.
    /// </summary>
    void Validate();

    /// <summary>
    /// Marks tthe associated data resource as invalid.
    /// </summary>
    void Invalidate();
}
