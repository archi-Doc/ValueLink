// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink.Integrality;

/// <summary>
/// Defines the default packet and iteration limits for synchronization.
/// </summary>
public static class IntegralityConstants
{
    /// <summary>
    /// Defines the default object-response limit in bytes: four mebibytes minus one kibibyte.
    /// </summary>
    public const int DefaultMaxMemoryLength = (1024 * 1024 * 4) - 1024; // ConnectionAgreement.MaxBlockSize

    /// <summary>
    /// Defines the default limit of three object-request iterations after probing.
    /// </summary>
    public const int DefaultMaxIntegrationCount = 3;
}
