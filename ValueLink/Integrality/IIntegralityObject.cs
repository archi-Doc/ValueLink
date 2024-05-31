// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand.IO;

namespace ValueLink.Integrality;

public interface IIntegralityObject
{// Exaltation of the Integrality by Baxter.
    void ClearIntegralityHash();

    ulong GetIntegralityHash();

    IntegralityResultMemory Differentiate(IIntegralityInternal engine, BytePool.RentMemory integration)
        => new(IntegralityResult.NotImplemented);

    void Compare(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer)
    {
    }

    void Integrate(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer)
    {
    }

    IntegralityResult Integrate(IIntegralityInternal engine, object? obj)
    => IntegralityResult.NotImplemented;
}
