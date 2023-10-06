// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

public static class RepeatableReadExtension
{
    public static bool IsValid(this RepeatableObjectState state)
        => state == RepeatableObjectState.Valid;

    public static bool IsInvalid(this RepeatableObjectState state)
        => state != RepeatableObjectState.Valid;

    public static bool IsValid(this RepeatableGoshujinState state)
        => state == RepeatableGoshujinState.Valid;

    public static bool IsInvalid(this RepeatableGoshujinState state)
        => state != RepeatableGoshujinState.Valid;
}
