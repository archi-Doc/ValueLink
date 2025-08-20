// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Defines an interface for releasing a lock on a data resource.
/// </summary>
public interface ILockableDataState
{
    /// <summary>
    /// Gets or sets the current state of the data resource.
    /// </summary>
    LockableDataState DataState { get; set; }
}
