// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;

namespace ValueLink.Integrality;

public delegate Task<IntegralityResultMemory> IntegralityBrokerDelegate(BytePool.RentMemory integration, CancellationToken cancellationToken);
