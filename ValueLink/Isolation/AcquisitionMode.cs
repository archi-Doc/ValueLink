// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Specify the data acquisition mode specifying get, create, or get-or-create behavior.
/// </summary>
public enum AcquisitionMode
{
    /// <summary>
    /// Retrieve the existing data. If an identifier is specified, obtain the data that matches the identifier.<br/>
    /// If it does not exist, it returns null.
    /// </summary>
    Get,

    /// <summary>
    /// Retrieve the existing data. If an identifier is specified, obtain the data that matches the identifier.<br/>
    /// If the data does not exist, it is created.
    /// </summary>
    GetOrCreate,

    /// <summary>
    /// Create the data. If an identifier is specified, create the data that matches the identifier.<br/>
    /// If it already exists, it returns null.
    /// </summary>
    Create,
}
