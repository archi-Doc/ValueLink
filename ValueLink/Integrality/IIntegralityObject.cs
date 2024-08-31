// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Tinyhand.IO;

namespace ValueLink.Integrality;

public interface IIntegralityObject
{// Exaltation of the Integrality by Baxter.
    void ClearIntegralityHash();

    ulong GetIntegralityHash();
}

public interface IIntegralityGoshujin
{
    public ulong TargetHash { get; set; }

    void Compare(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer);

    void Integrate(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer);

    BytePool.RentMemory Differentiate(ReadOnlyMemory<byte> integration, int maxItems, int maxMemoryLength = IntegralityConstants.DefaultMaxMemoryLength);

    IntegralityResult IntegrateObject(IIntegralityInternal engine, object? obj);
}
