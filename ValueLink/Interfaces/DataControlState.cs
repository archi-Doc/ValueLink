// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace ValueLink;

/// <summary>
/// Represents a set of control state flags associated with a data object.
/// </summary>
[Flags]
public enum DataControlState : byte
{
    /// <summary>
    /// Indicates the object is pinned and guaranteed to remain only in memory<br/> and will never be released and written to disk.
    /// </summary>
    Pinned = 1,

    /// <summary>
    /// The data has been invalidated and can no longer be used.<br/>
    /// Use this before deleting the parent or for other similar purposes.
    /// </summary>
    Invalid = 2,
}
