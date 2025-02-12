// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink.Integrality;

/// <summary>
/// Represents the result of an integrality operation.
/// </summary>
public enum IntegralityResult : byte
{
    /// <summary>
    /// The integration was successful.
    /// </summary>
    Success,

    /// <summary>
    /// The integration operation is incomplete due to insufficient or inconsistent data.
    /// </summary>
    Incomplete,

    /// <summary>
    /// The provided data was not integrated because it is broken, does not pass validation, or is outdated.
    /// </summary>
    InvalidData,

    /// <summary>
    /// The operation failed because the object limit was reached.
    /// </summary>
    LimitExceeded,
}
