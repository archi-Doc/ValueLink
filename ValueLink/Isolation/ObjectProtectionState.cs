// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the protection state of the object.
/// </summary>
public enum ObjectProtectionState : byte
{
    /// <summary>
    /// The object is not protected (can be deleted).
    /// </summary>
    Unprotected,

    /// <summary>
    /// The object is protected (cannot be deleted).
    /// </summary>
    Protected,

    /// <summary>
    /// The object is pending deletion (scheduled to be removed).
    /// </summary>
    PendingDeletion,

    /// <summary>
    /// The object has already been deleted.
    /// </summary>
    Deleted,
}
