﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace ValueLink.Integrality;

public readonly struct IntegralityResultMemory
{
    public IntegralityResultMemory(IntegralityResult result, BytePool.RentMemory difference)
    {
        this.Result = result;
        this.RentMemory = difference;
    }

    /*public IntegralityResultMemory(BytePool.RentMemory difference)
    {
        this.Result = IntegralityResult.Success;
        this.RentMemory = difference;
    }*/

    public IntegralityResultMemory(IntegralityResult result)
    {
        this.Result = result;
        this.RentMemory = default;
    }

    public readonly IntegralityResult Result;

    public readonly BytePool.RentMemory RentMemory;

    public void Return()
        => this.RentMemory.Return();
}