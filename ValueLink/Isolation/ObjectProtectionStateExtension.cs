// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ValueLink;

public static class ObjectProtectionStateExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsObsolete(this ObjectProtectionState state)
    {
        return state == ObjectProtectionState.Deleted || state == ObjectProtectionState.PendingDeletion;
    }
}
