// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink.Integrality;

/// <summary>
/// A structure representing the result of the integration and various counts.
/// </summary>
public readonly record struct IntegralityResultAndCount
{
    /// <summary>
    /// Gets the result of the integration.
    /// </summary>
    public readonly IntegralityResult Result;

    /// <summary>
    /// Gets the number of iterations performed during the integration.
    /// </summary>
    public readonly int IterationCount;

    /// <summary>
    /// Gets the count of successfully integrated items.
    /// </summary>
    public readonly int IntegratedCount;

    /// <summary>
    /// Gets the count of trimmed items during the integration.
    /// </summary>
    public readonly int TrimmedCount;

    /// <summary>
    /// Gets a value indicating whether the integration result is successful.
    /// </summary>
    public bool IsSuccess => this.Result == IntegralityResult.Success;

    /// <summary>
    /// Gets a value indicating whether any items were modified during the integration.
    /// </summary>
    public bool IsModified => this.IntegratedCount > 0 || this.TrimmedCount > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegralityResultAndCount"/> struct with the specified result.
    /// </summary>
    /// <param name="result">The result of the integration.</param>
    public IntegralityResultAndCount(IntegralityResult result)
    {
        this.Result = result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegralityResultAndCount"/> struct with the specified result, iteration count, integrated count, and trimmed count.
    /// </summary>
    /// <param name="result">The result of the integration.</param>
    /// <param name="iterationCount">The number of iterations performed during the integration.</param>
    /// <param name="integratedCount">The count of successfully integrated items.</param>
    /// <param name="trimmedCount">The count of trimmed items during the integration.</param>
    public IntegralityResultAndCount(IntegralityResult result, int iterationCount, int integratedCount, int trimmedCount)
    {
        this.Result = result;
        this.IterationCount = iterationCount;
        this.IntegratedCount = integratedCount;
        this.TrimmedCount = trimmedCount;
    }
}
