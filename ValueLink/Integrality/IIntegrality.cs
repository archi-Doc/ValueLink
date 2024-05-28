// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;

namespace ValueLink.Integrality;

public interface IIntegrality
{// Exaltation of the Integrality by Baxter.
    void ClearIntegralityHash();

    ulong GetIntegralityHash();

    DifferentiateResult Differentiate(BytePool.RentMemory integration)
        => default;

    DifferentiateResult Integrate(BytePool.RentMemory difference)
        => default;
}
