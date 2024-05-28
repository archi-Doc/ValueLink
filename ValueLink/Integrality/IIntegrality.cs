// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Tinyhand.IO;

namespace ValueLink.Integrality;

public interface IIntegrality
{// Exaltation of the Integrality by Baxter.
    void ClearIntegralityHash();

    ulong GetIntegralityHash();

    IntegralityResultMemory Differentiate(BytePool.RentMemory integration);

    void Integrate(IntegralityEngine engine, ref TinyhandReader reader, ref TinyhandWriter writer);
}
