// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace ValueLink;

/// <summary>
/// ValueLink Global variables.
/// </summary>
public static class ValueLinkGlobal
{
    private static TimeSpan lockTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the default lock timeout, in milliseconds.<br/>
    /// This is the same setting as <see cref="LockTimeout"/>, expressed in milliseconds.
    /// </summary>
    public static int LockTimeoutInMilliseconds
    {
        get => (int)lockTimeout.TotalMilliseconds;
        set => lockTimeout = TimeSpan.FromMilliseconds(value);
    }

    /// <summary>
    /// Gets or sets the default lock timeout.<br/>
    /// This is the same setting as <see cref="LockTimeoutInMilliseconds"/>.
    /// </summary>
    public static TimeSpan LockTimeout
    {
        get => lockTimeout;
        set => lockTimeout = value;
    }
}
