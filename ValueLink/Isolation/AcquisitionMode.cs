// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Specify the data acquisition mode specifying get, create, or get-or-create behavior.
/// </summary>
public enum AcquisitionMode
{
    /// <summary>
    /// Retrieve the existing data. If an identifier is specified, obtain the data that matches the identifier.<br/>
    /// If it does not exist, it returns null.<br/>
    /// TryLock() -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.NotFound"/> or <see cref="DataScopeResult.Obsolete"/>.
    /// </summary>
    GetOnly,

    /// <summary>
    /// Retrieve the existing data. If an identifier is specified, obtain the data that matches the identifier.<br/>
    /// If the data does not exist, it is created.<br/>
    /// TryLock() -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.Created"/> or <see cref="DataScopeResult.Obsolete"/>.
    /// </summary>
    GetOrCreate,

    /// <summary>
    /// Create the data. If an identifier is specified, create the data that matches the identifier.<br/>
    /// If it already exists, it returns null.<br/>
    /// TryLock() -> <see cref="DataScopeResult.Created"/> or <see cref="DataScopeResult.AlreadyExists"/> or <see cref="DataScopeResult.Obsolete"/>.
    /// </summary>
    CreateOnly,

    /// <summary>
    /// Only retrieves an existing object, regardless of whether it is valid or invalid.
    /// If it does not exist, it returns null.<br/>
    /// TryLock() -> <see cref="DataScopeResult.Retrieved"/> or <see cref="DataScopeResult.NotFound"/>.
    /// </summary>
    GetOnlyIgnoreState,
}
