// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace ValueLink;

/// <summary>
/// Exposes the owner lock used by read-committed operations.
/// </summary>
public interface IReadCommittedSemaphore
{
    Lock LockObject { get; }
}
