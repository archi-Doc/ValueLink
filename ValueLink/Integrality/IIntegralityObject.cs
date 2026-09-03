// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Tinyhand.IO;

namespace ValueLink.Integrality;

/// <summary>
/// Provides a cached content hash for incremental synchronization.
/// </summary>
public interface IIntegralityObject
{
    /// <summary>
    /// Invalidates the cached content hash. Generated object implementations also invalidate their owner's hash.
    /// </summary>
    void ClearIntegralityHash();

    /// <summary>
    /// Returns the cached content hash, computing it when needed.
    /// </summary>
    /// <returns>The 64-bit content hash.</returns>
    ulong GetIntegralityHash();
}

/// <summary>
/// Implements the generated owner's incremental synchronization protocol.
/// </summary>
public interface IIntegralityGoshujin : IIntegralityObject
{
    /// <summary>
    /// Compares a remote key list, applies the removal policy, and writes requests for differing objects.
    /// </summary>
    /// <param name="engine">The synchronization limits and validation policy.</param>
    /// <param name="reader">The reader positioned after the incoming packet header.</param>
    /// <param name="writer">The writer for outgoing object requests.</param>
    void Compare(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer);

    /// <summary>
    /// Applies object responses and writes requests for objects still missing.
    /// </summary>
    /// <param name="engine">The synchronization limits and validation policy.</param>
    /// <param name="reader">The reader positioned after the incoming packet header.</param>
    /// <param name="writer">The writer for outgoing object requests.</param>
    /// <param name="integratedCount">The running count of accepted objects, updated by this operation.</param>
    void Integrate(IIntegralityInternal engine, ref TinyhandReader reader, ref TinyhandWriter writer, ref int integratedCount);

    /// <summary>
    /// Creates a response packet. The caller owns the returned buffer and must return it after use.
    /// </summary>
    /// <param name="engine">The synchronization limits and validation policy.</param>
    /// <param name="integration">The incoming synchronization request.</param>
    /// <returns>An owned response buffer to return after use.</returns>
    BytePool.RentMemory Differentiate(IIntegralityInternal engine, ReadOnlyMemory<byte> integration);

    /// <summary>
    /// Validates and adds or replaces a single object, returning the outcome.
    /// </summary>
    /// <param name="engine">The synchronization limits and validation policy.</param>
    /// <param name="obj">The object to validate and integrate.</param>
    /// <returns>The validation or integration outcome.</returns>
    IntegralityResult IntegrateObject(IIntegralityInternal engine, object? obj);
}
