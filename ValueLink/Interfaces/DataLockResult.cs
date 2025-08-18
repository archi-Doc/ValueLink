// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the result of a data lock attempt.
/// </summary>
public enum DataLockResult
{
    /// <summary>
    /// The lock was successfully acquired.
    /// </summary>
    Success,

    /// <summary>
    /// The lock attempt failed because the operation timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The lock attempt is no longer valid because the resource has become obsolete or invalidated.
    /// </summary>
    Obsolete,
}
