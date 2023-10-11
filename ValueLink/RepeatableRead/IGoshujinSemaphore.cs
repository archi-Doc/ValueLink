// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

public interface IGoshujinSemaphore
{
    public object SyncObject { get; }

    public GoshujinState State { get; set; }

    public int SemaphoreCount { get; set; }

    public bool IsValid
        => this.State == GoshujinState.Valid;

    public bool CanUnload
        => this.State == GoshujinState.Valid && this.SemaphoreCount == 0;

    /// <summary>
    /// Try to acquire the resource.<br/>
    /// You can call this method multiple times, but the maximum count for acquiring resources is 1.
    /// </summary>
    /// <param name="count">The count of resources acquired at the current moment.</param>
    /// <returns>true: success, false: failure/invalid.</returns>
    public bool TryAcquire(ref int count)
    {
        if (!this.IsValid)
        {// Invalid (Unloading/Obsolete)
            this.SemaphoreCount -= count;
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

    public bool TryAcquireOne()
    {
        if (!this.IsValid)
        {// Invalid (Unloading/Obsolete)
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

    public bool LockAndTryUnload(out GoshujinState state)
    {
        var result = false;
        lock (this.SyncObject)
        {
            if (!this.IsValid)
            {// Invalid (Unloading/Obsolete)
            }
            else
            {// Valid
                this.State = GoshujinState.Unloading;
                if (this.SemaphoreCount > 0)
                {// Acquired
                }
                else
                {// Can unload
                    result = true;
                }
            }

            state = this.State;
        }

        return result;
    }

    public void SetObsolete()
    {
        this.State = GoshujinState.Obsolete;
    }

    public void SetUnloading()
    {
        this.State = GoshujinState.Unloading;
    }

    public void LockAndForceUnload()
    {
        lock (this.SyncObject)
        {
            this.State = GoshujinState.Unloading;
        }
    }
}
