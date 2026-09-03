// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;

namespace ValueLink.Integrality;

/// <summary>
/// Sends a synchronization request and transfers ownership of the response buffer to the engine.
/// </summary>
/// <param name="integration">Request bytes valid only until the returned task completes.</param>
/// <param name="cancellationToken">The cancellation token to forward to the transport.</param>
/// <returns>A response buffer that the engine will return after processing.</returns>
public delegate Task<BytePool.RentMemory> IntegralityBrokerDelegate(ReadOnlyMemory<byte> integration, CancellationToken cancellationToken);
