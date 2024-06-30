// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;

namespace ValueLink.Integrality;

public static class IntegralityResultHelper
{
    static IntegralityResultHelper()
    {
        BytePool.RentMemory rentMemory;

        rentMemory = BytePool.Default.Rent(1).AsMemory();
        MemoryMarshal.TryWrite(rentMemory.Span, (byte)IntegralityResult.NotImplemented);
        NotImplemented = rentMemory;

        rentMemory = BytePool.Default.Rent(1).AsMemory();
        MemoryMarshal.TryWrite(rentMemory.Span, (byte)IntegralityResult.InvalidData);
        InvalidData = rentMemory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetError(BytePool.RentMemory rentMemory, out IntegralityResult result)
    {
        if (rentMemory.Length == 0)
        {
            result = IntegralityResult.InvalidData;
        }

        result = (IntegralityResult)rentMemory.Span[rentMemory.Length - 1];
        return result != IntegralityResult.Success;
    }

    public static readonly BytePool.RentMemory NotImplemented;

    public static readonly BytePool.RentMemory InvalidData;
}
