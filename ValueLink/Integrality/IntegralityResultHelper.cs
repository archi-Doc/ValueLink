// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace ValueLink.Integrality;

/// <summary>
/// Encodes shared status responses and distinguishes them from protocol payloads.
/// </summary>
public static class IntegralityResultHelper
{
    static IntegralityResultHelper()
    {
        byte[] bytes;

        bytes = [(byte)IntegralityResult.Incomplete,];
        // The memory overload wraps ordinary storage without a shared reference counter.
        IncompleteMemory = BytePool.RentedMemory.CreateFrom(bytes.AsMemory());

        bytes = [(byte)IntegralityResult.InvalidData,];
        InvalidDataMemory = BytePool.RentedMemory.CreateFrom(bytes.AsMemory());

        // bytes = new byte[] { (byte)IntegralityResult.NotImplemented, };
        // NotImplemented = BytePool.RentArray.CreateFrom(bytes).AsMemory();
    }

    /// <summary>
    /// Parses the result encoded in a response buffer.
    /// </summary>
    /// <param name="rentedMemory">The response buffer: empty is invalid, a single byte is a status response, and anything longer is a protocol payload.</param>
    /// <param name="result">Receives <see cref="IntegralityResult.Success"/> for a payload, the encoded status for a single byte, or <see cref="IntegralityResult.InvalidData"/> for an empty buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ParseResult(BytePool.RentedMemory rentedMemory, out IntegralityResult result)
    {
        if (rentedMemory.Length == 0)
        {
            result = IntegralityResult.InvalidData;
        }
        else if (rentedMemory.Length == 1)
        {
            result = (IntegralityResult)rentedMemory.Span[0];
        }
        else
        {
            result = IntegralityResult.Success;
        }
    }

    /// <summary>
    /// A shared single-byte response that encodes <see cref="IntegralityResult.Incomplete"/>.
    /// </summary>
    public static readonly BytePool.RentedMemory IncompleteMemory;

    /// <summary>
    /// A shared single-byte response that encodes <see cref="IntegralityResult.InvalidData"/>.
    /// </summary>
    public static readonly BytePool.RentedMemory InvalidDataMemory;

    // public static readonly BytePool.RentedMemory NotImplemented;
}
