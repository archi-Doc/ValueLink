// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the state of the goshujin.
/// </summary>
public enum RepeatableGoshujinState
{
    /// <summary>
    /// The goshujin is in a valid state.
    /// </summary>
    Valid,

    /// <summary>
    /// The goshujin is unloading.
    /// </summary>
    Unloading,

    /// <summary>
    /// The goshujin is not in a valid state because it has been unloaded.
    /// </summary>
    Obsolete,
}
