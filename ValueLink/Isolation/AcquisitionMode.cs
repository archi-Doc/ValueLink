// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Selects whether acquisition retrieves existing data or creates new data.
/// </summary>
public enum AcquisitionMode
{
    /// <summary>
    /// Retrieves existing data and fails when it is absent.
    /// </summary>
    GetOnly,

    /// <summary>
    /// Retrieves existing data or creates it when absent.
    /// </summary>
    GetOrCreate,

    /// <summary>
    /// Creates new data and fails when it already exists.
    /// </summary>
    CreateOnly,

    /// <summary>
    /// Requests existing data while allowing adapter-specific state checks to be skipped.
    /// Does not bypass missing data or an invalid owner.
    /// </summary>
    GetOnlyIgnoreState,
}
