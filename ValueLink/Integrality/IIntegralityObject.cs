// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand.IO;

namespace ValueLink.Integrality;

public interface IIntegralityObject
{// Exaltation of the Integrality by Baxter.
    void ClearIntegralityHash();

    ulong GetIntegralityHash();

    IntegralityResultMemory Differentiate(Integrality engine, BytePool.RentMemory integration)
        => new(IntegralityResult.NotImplemented);

    void Compare(Integrality engine, ref TinyhandReader reader, ref TinyhandWriter writer)
    {
    }

    void Integrate(Integrality engine, ref TinyhandReader reader, ref TinyhandWriter writer)
    {
    }

    IntegralityResult Integrate(Integrality engine, object obj)
    => IntegralityResult.NotImplemented;
}
