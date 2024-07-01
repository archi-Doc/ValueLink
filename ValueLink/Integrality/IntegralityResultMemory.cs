// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand;

namespace ValueLink.Integrality;

// [TinyhandObject]
public readonly partial struct IntegralityResultMemory
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

    // [Key(0)]
    public readonly IntegralityResult Result;

    // [Key(1)]
    public readonly BytePool.RentMemory RentMemory;

    public void Return()
        => this.RentMemory.Return();
}
