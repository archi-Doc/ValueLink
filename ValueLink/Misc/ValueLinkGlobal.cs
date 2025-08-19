// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace ValueLink;

/// <summary>
/// ValueLink Global variables.
/// </summary>
public static class ValueLinkGlobal
{
    public static int LockTimeoutInMilliseconds { get; set; } = 1_000;

    public static TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
