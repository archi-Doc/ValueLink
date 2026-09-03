// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace ValueLink;

/// <summary>
/// Exposes the semaphore used to serialize access to an owner.
/// </summary>
public interface ISerializableSemaphore
{
    public SemaphoreLock LockObject { get; }
}
