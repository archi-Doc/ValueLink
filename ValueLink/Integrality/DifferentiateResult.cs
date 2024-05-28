// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace ValueLink.Integrality;

public readonly struct DifferentiateResult
{
    public DifferentiateResult(IntegralityResult result, BytePool.RentMemory difference)
    {
        this.Result = result;
        this.RentMemory = difference;
    }

    public readonly IntegralityResult Result;

    public readonly BytePool.RentMemory RentMemory;

    public void Return()
        => this.RentMemory.Return();
}
