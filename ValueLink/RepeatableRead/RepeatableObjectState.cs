// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the state of the object.
/// </summary>
public enum RepeatableObjectState
{
    /// <summary>
    /// The object is in a valid state.
    /// </summary>
    Valid,

    /// <summary>
    /// The object is not in a valid state because it has been removed or updated.
    /// </summary>
    Obsolete,

    /// <summary>
    /// The object has been created, but it is not in a valid state because it is before the commit.
    /// </summary>
    Created,
}
