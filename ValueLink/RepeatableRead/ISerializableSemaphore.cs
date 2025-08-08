// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace ValueLink;

public interface ISerializableSemaphore
{
    public SemaphoreLock LockObject { get; }

    public GoshujinState State { get; set; } // Lock:LockObject
}
