// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace ValueLink;

public interface IReadCommittedSemaphore
{
    Lock LockObject { get; }
}
