// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace ValueLink;

public interface ISerializableSemaphore
{
    public SemaphoreLock LockObject { get; }

    public GoshujinState State { get; set; } // Lock:LockObject

    public void SetObsolete()
        => this.State = GoshujinState.Obsolete;

    public void SetReleasing()
        => this.State = GoshujinState.Releasing;
}
