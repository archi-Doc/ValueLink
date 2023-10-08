// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

public interface IGoshujinSemaphore
{
    public object SyncObject { get; }

    public int SemaphoreCount { get; set; }

    public bool IsValid
        => this.SemaphoreCount >= 0;

    public bool CanUnload
        => this.SemaphoreCount == 0;

    public bool TryAcquire()
    {
        if (this.SemaphoreCount < 0)
        {
            return false;
        }
        else
        {
            this.SemaphoreCount++;
            return true;
        }
    }

    public bool LockAndTryAcquire()
    {
        lock (this.SyncObject)
        {
            return this.TryAcquire();
        }
    }

    public void Release(int count)
    {
        this.SemaphoreCount -= count;
    }

    public void LockAndRelease(int count)
    {
        lock (this.SyncObject)
        {
            this.Release(count);
        }
    }

    public bool TryUnload()
    {
        if (this.SemaphoreCount > 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void ForceUnload()
    {
        this.SemaphoreCount = -1;
    }
}
