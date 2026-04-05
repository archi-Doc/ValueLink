// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Defines members for releasing a lock and managing the control state of an associated data resource.
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
    /// Gets the current control state of the associated data resource.
    /// </summary>
    /// <returns>
    /// The current <see cref="DataControlState" /> value.
    /// </returns>
    DataControlState GetControlState();

    /// <summary>
    /// Sets the control state of the associated data resource.
    /// </summary>
    /// <param name="state">
    /// The <see cref="DataControlState" /> to apply.
    /// </param>
    void SetControlState(DataControlState state);
}
