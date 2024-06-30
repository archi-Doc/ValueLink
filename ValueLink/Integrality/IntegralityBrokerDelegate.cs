// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValueLink.Integrality;

public delegate Task<IntegralityResultMemory> IntegralityBrokerDelegate(ReadOnlySpan<byte> integration, CancellationToken cancellationToken);
