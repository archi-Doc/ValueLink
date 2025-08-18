// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace ValueLink;

public interface IReadCommittedSemaphore
{
    public Lock LockObject { get; }

    public GoshujinState State { get; set; } // Lock:LockObject

    public bool IsValid
        => this.State == GoshujinState.Valid;

    public bool CanRelease
        => this.State == GoshujinState.Valid;

    public void SetObsolete()
        => this.State = GoshujinState.Obsolete;

    public void SetReleasing()
        => this.State = GoshujinState.Releasing;
}
