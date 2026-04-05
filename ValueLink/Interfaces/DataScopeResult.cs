// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Represents the result of a data lock attempt.
/// </summary>
public enum DataScopeResult
{
    /// <summary>
    /// The object was not found, so the requested retrieval could not be performed.<br/>
    /// <see cref="AcquisitionMode.GetOnly" /> -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.NotFound"/>.
    /// </summary>
    NotFound,

    /// <summary>
    /// An existing object was successfully retrieved.<br/>
    /// <see cref="AcquisitionMode.GetOnly" /> -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.NotFound"/>.<br/>
    /// <see cref="AcquisitionMode.GetOrCreate" /> -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.Created"/>.
    /// </summary>
    Retrieved,

    /// <summary>
    /// A new object was successfully created.<br/>
    /// <see cref="AcquisitionMode.GetOrCreate" /> -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.Created"/>.<br/>
    /// <see cref="AcquisitionMode.CreateOnly" /> -> <see cref="DataScopeResult.Created"/> or <see cref="DataScopeResult.AlreadyExists"/>.
    /// </summary>
    Created,

    /// <summary>
    /// The specified data already exists.<br/>
    /// <see cref="AcquisitionMode.CreateOnly" /> -> <see cref="DataScopeResult.Created"/> or <see cref="DataScopeResult.AlreadyExists"/>.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// The object was deleted successfully.
    /// </summary>
    Deleted,

    /// <summary>
    /// The deletion operation timed out, and the object was force-deleted.
    /// </summary>
    ForceDeleted,

    /// <summary>
    /// The lock attempt failed because the operation timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The lock attempt is no longer valid because the resource has become obsolete or invalidated.
    /// </summary>
    Obsolete,

    /// <summary>
    /// The lock attempt failed because the object is in an unlockable state.
    /// </summary>
    Unlockable,

    /// <summary>
    /// Failed to acquire lock due to the storage engine being shut down.
    /// </summary>
    Rip,
}
