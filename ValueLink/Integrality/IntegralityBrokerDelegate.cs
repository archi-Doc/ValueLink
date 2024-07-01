// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;

namespace ValueLink.Integrality;

public delegate Task<BytePool.RentMemory> IntegralityBrokerDelegate(ReadOnlyMemory<byte> integration, CancellationToken cancellationToken);
