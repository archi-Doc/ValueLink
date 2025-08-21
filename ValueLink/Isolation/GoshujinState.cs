// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the state of the goshujin.
/// </summary>
public enum GoshujinState
{
    /// <summary>
    /// The goshujin is in a valid state.
    /// </summary>
    Valid,

    /// <summary>
    /// The goshujin is in the process of being released.
    /// </summary>
    Releasing,

    /// <summary>
    /// The goshujin is not in a valid state because it has been deleted.
    /// </summary>
    Obsolete,
}
