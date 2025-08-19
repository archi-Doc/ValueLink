// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the result of a data lock attempt.
/// </summary>
public enum DataScopeResult
{
    /// <summary>
    /// The lock was successfully acquired.
    /// </summary>
    Success,

    /// <summary>
    /// The specified data was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The specified data already exists.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// The lock attempt failed because the operation timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The lock attempt is no longer valid because the resource has become obsolete or invalidated.
    /// </summary>
    Obsolete,

    /// <summary>
    /// Failed to acquire lock due to the storage engine being shut down.
    /// </summary>
    Rip,
}
