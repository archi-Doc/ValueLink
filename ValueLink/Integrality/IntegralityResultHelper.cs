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
        var bytes = new byte[] { (byte)IntegralityResult.NotImplemented, };
        NotImplemented = BytePool.RentArray.CreateStatic(bytes).AsMemory();

        bytes = new byte[] { (byte)IntegralityResult.InvalidData, };
        InvalidData = BytePool.RentArray.CreateStatic(bytes).AsMemory();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> TryGet(BytePool.RentMemory rentMemory, out IntegralityResult result)
    {
        if (rentMemory.Length == 0)
        {
            result = IntegralityResult.InvalidData;
            return default;
        }

        var length = rentMemory.Length - 1;
        result = (IntegralityResult)rentMemory.Span[length];
        return rentMemory.Span.Slice(0, length);
    }

    public static readonly BytePool.RentMemory NotImplemented;

    public static readonly BytePool.RentMemory InvalidData;
}
