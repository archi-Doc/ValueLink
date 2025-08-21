// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Specify the behavior of lock function to either retrieve an existing object or create a new one if it does not exist.
/// </summary>
public enum LockMode
{
    /// <summary>
    /// Retrieves the object that matches the specified key and attempts to lock it.<br/>
    /// If it does not exist, it returns null.
    /// </summary>
    Get,

    /// <summary>
    /// Creates an object with the specified key and attempts to lock it.<br/>
    /// If it already exists, it returns null.
    /// </summary>
    Create,

    /// <summary>
    /// Retrieves the object that matches the specified key, or creates it if it does not exist, and attempts to lock it.
    /// </summary>
    GetOrCreate,
}
