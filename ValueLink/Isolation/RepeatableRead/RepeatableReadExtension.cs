// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

public static class RepeatableReadExtension
{
    public static bool IsValid(this RepeatableReadObjectState state)
        => state == RepeatableReadObjectState.Valid;

    public static bool IsInvalid(this RepeatableReadObjectState state)
        => state != RepeatableReadObjectState.Valid;
}
