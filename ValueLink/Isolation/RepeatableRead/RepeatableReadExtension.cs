// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Tests whether a repeatable-read object is a committed, current instance.
/// </summary>
public static class RepeatableReadExtension
{
    public static bool IsValid(this RepeatableReadObjectState state)
        => state == RepeatableReadObjectState.Valid;

    public static bool IsInvalid(this RepeatableReadObjectState state)
        => state != RepeatableReadObjectState.Valid;
}
