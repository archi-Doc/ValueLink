// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Tests whether a repeatable-read object is a committed, current instance.
/// </summary>
public static class RepeatableReadObjectStateExtensions
{
    /// <summary>
    /// Determines whether the record is a committed, current instance.
    /// </summary>
    /// <param name="state">The record state.</param>
    /// <returns>True only for Valid.</returns>
    public static bool IsValid(this RepeatableReadObjectState state)
        => state == RepeatableReadObjectState.Valid;

    /// <summary>
    /// Determines whether the record is uncommitted or obsolete.
    /// </summary>
    /// <param name="state">The record state.</param>
    /// <returns>True for every state other than Valid.</returns>
    public static bool IsInvalid(this RepeatableReadObjectState state)
        => state != RepeatableReadObjectState.Valid;
}
