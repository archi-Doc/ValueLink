// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;

namespace ValueLink.Integrality;

internal static class IntegralityHelper
{
    static IntegralityHelper()
    {
        /*ProbePacket = BytePool.Default.Rent(sizeof(byte)).AsMemory(0, sizeof(byte));
        ProbePacket.Span[0] = (byte)IntegralityState.Probe;*/
    }

    // public static readonly BytePool.RentMemory ProbePacket;
}
