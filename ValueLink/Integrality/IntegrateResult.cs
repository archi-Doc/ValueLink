// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink.Integrality;

public readonly record struct IntegrateResult
{
    /// <summary>
    /// Gets the result of the integration.
    /// </summary>
    public readonly IntegralityResult Result;

    /// <summary>
    /// Gets the count of successfully integrated items.
    /// </summary>
    public readonly int IntegratedCount;

    /// <summary>
    /// Gets the count of trimmed items during the integration.
    /// </summary>
    public readonly int TrimmedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrateResult"/> struct with the specified result.
    /// </summary>
    /// <param name="result">The result of the integration.</param>
    public IntegrateResult(IntegralityResult result)
    {
        this.Result = result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrateResult"/> struct with the specified result, integrated count, and trimmed count.
    /// </summary>
    /// <param name="result">The result of the integration.</param>
    /// <param name="integratedCount">The count of successfully integrated items.</param>
    /// <param name="trimmedCount">The count of trimmed items during the integration.</param>
    public IntegrateResult(IntegralityResult result, int integratedCount, int trimmedCount)
    {
        this.Result = result;
        this.IntegratedCount = integratedCount;
        this.TrimmedCount = trimmedCount;
    }
}
