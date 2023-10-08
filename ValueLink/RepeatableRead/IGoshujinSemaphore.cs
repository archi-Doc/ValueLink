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

    /// <summary>
    /// Try to acquire the resource.<br/>
    /// You can call this method multiple times, but the maximum count for acquiring resources is 1.
    /// </summary>
    /// <param name="count">The count of resources acquired at the current moment.</param>
    /// <returns>true: success, false: failure/invalid.</returns>
    public bool TryAcquire(ref int count)
    {
        if (this.SemaphoreCount < 0)
        {// Invalid
            count = 0;
            return false;
        }
        else if (count > 0)
        {// Already acquired
            return true;
        }
        else
        {// Acquire 1
            this.SemaphoreCount++;
            count = 1;
            return true;
        }
    }

    /*public bool LockAndTryAcquire(ref int count)
    {
        lock (this.SyncObject)
        {
            return this.TryAcquire(ref count);
        }
    }*/

    public bool TryAcquireOne()
    {
        if (this.SemaphoreCount < 0)
        {// Invalid
            return false;
        }
        else
        {// Acquire 1
            this.SemaphoreCount++;
            return true;
        }
    }

    public bool LockAndTryAcquireOne()
    {
        lock (this.SyncObject)
        {
            return this.TryAcquireOne();
        }
    }

    public void ReleaseOne()
    {
        this.SemaphoreCount--;
    }

    public void LockAndReleaseOne()
    {
        lock (this.SyncObject)
        {
            this.ReleaseOne();
        }
    }

    /// <summary>
    /// Release the acquired resource.<br/>
    /// </summary>
    /// <param name="count">The count of resources acquired.</param>
    public void Release(ref int count)
    {
        this.SemaphoreCount -= count;
        count = 0;
    }

    public void LockAndRelease(ref int count)
    {
        if (count == 0)
        {
            return;
        }

        lock (this.SyncObject)
        {
            this.Release(ref count);
        }
    }

    public bool TryUnload()
    {
        if (this.SemaphoreCount > 0)
        {// Acquired
            return false;
        }
        else
        {// Can unload
            this.SemaphoreCount = -1;
            return true;
        }
    }

    public void ForceUnload()
    {
        this.SemaphoreCount = -1;
    }
}
