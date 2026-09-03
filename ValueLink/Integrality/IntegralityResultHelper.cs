// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace ValueLink.Integrality;

public static class IntegralityResultHelper
{
    static IntegralityResultHelper()
    {
        byte[] bytes;

        bytes = [(byte)IntegralityResult.Incomplete,];
        // The memory overload wraps ordinary storage without a shared reference counter.
        Incomplete = BytePool.RentMemory.CreateFrom(bytes.AsMemory());

        bytes = [(byte)IntegralityResult.InvalidData,];
        InvalidData = BytePool.RentMemory.CreateFrom(bytes.AsMemory());

        // bytes = new byte[] { (byte)IntegralityResult.NotImplemented, };
        // NotImplemented = BytePool.RentArray.CreateFrom(bytes).AsMemory();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ParseMemoryAndResult(BytePool.RentMemory rentMemory, out IntegralityResult result)
    {
        if (rentMemory.Length == 0)
        {
            result = IntegralityResult.InvalidData;
        }
        else if (rentMemory.Length == 1)
        {
            result = (IntegralityResult)rentMemory.Span[0];
        }
        else
        {
            result = IntegralityResult.Success;
        }
    }

    public static readonly BytePool.RentMemory Incomplete;

    public static readonly BytePool.RentMemory InvalidData;

    // public static readonly BytePool.RentMemory NotImplemented;
}
