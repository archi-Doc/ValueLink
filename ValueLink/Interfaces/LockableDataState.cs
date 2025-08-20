// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the state of a lockable data resource.
/// </summary>
public enum LockableDataState
{
    /// <summary>
    /// The data is not protected and can be deleted.
    /// </summary>
    Unprotected,

    /// <summary>
    /// The data is protected from deletion.
    /// </summary>
    Protected,

    /// <summary>
    /// The data has been deleted and is no longer available.
    /// </summary>
    Deleted,
}
