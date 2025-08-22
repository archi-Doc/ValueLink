// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the protection state of the object.
/// </summary>
public enum ObjectProtectionState
{
    /// <summary>
    /// The object is not protected (can be deleted).
    /// </summary>
    Unprotected,

    /// <summary>
    /// The object is protected.
    /// </summary>
    Protected,

    /// <summary>
    /// The object is deleted.
    /// </summary>
    Deleted,
}
