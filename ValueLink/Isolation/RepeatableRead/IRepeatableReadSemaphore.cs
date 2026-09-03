// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace ValueLink;

/// <summary>
/// Tracks active writers and the release state of a repeatable-read owner.
/// </summary>
/// <remarks>
/// Hold LockObject when calling methods without a LockAnd prefix or changing State or SemaphoreCount directly.
/// </remarks>
public interface IRepeatableReadSemaphore
{
    /// <summary>
    /// Gets the lock protecting owner state and writer counts.
    /// </summary>
    public Lock LockObject { get; }

    /// <summary>
    /// Gets or sets the owner's lifecycle state while holding LockObject.
    /// </summary>
    public GoshujinState State { get; set; } // Lock:LockObject

    /// <summary>
    /// Gets or sets the active acquisition count while holding LockObject.
    /// </summary>
    public int SemaphoreCount { get; set; } // Lock:LockObject

    /// <summary>
    /// Gets a value indicating whether the owner still accepts acquisitions.
    /// </summary>
    public bool IsValid
        => this.State == GoshujinState.Valid;

    /// <summary>
    /// Gets a value indicating whether the owner is valid and has no active acquisitions.
    /// </summary>
    public bool CanRelease
        => this.State == GoshujinState.Valid && this.SemaphoreCount == 0;

    /// <summary>
    /// Acquires at most one reference for this counter, or releases it if the owner is invalid.
    /// </summary>
    /// <param name="count">The count of resources acquired at the current moment.</param>
    /// <returns>true: success, false: failure/invalid.</returns>
    public bool TryAcquire(ref int count)
    {
        if (!this.IsValid)
        {// Invalid (Releasing/Obsolete)
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

    /// <summary>
    /// Increments the acquisition count if the owner is valid. The caller must hold LockObject.
    /// </summary>
    /// <returns>True if one reference was acquired; false if the owner is invalid.</returns>
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

    /// <summary>
    /// Locks the owner and acquires one reference if it is valid.
    /// </summary>
    /// <returns>True if one reference was acquired; false if the owner is invalid.</returns>
    public bool LockAndTryAcquireOne()
    {
        using (this.LockObject.EnterScope())
        {
            return this.TryAcquireOne();
        }
    }

    /// <summary>
    /// Releases one acquired reference. The caller must hold LockObject.
    /// </summary>
    public void ReleaseOne()
    {
        this.SemaphoreCount--;
    }

    /// <summary>
    /// Locks the owner and releases one acquired reference.
    /// </summary>
    public void LockAndReleaseOne()
    {
        using (this.LockObject.EnterScope())
        {
            this.ReleaseOne();
        }
    }

    /// <summary>
    /// Releases the supplied acquisition count and resets it to zero. The caller must hold LockObject.
    /// </summary>
    /// <param name="count">The count of resources acquired.</param>
    public void Release(ref int count)
    {
        this.SemaphoreCount -= count;
        count = 0;
    }

    /// <summary>
    /// Locks the owner, releases the supplied acquisition count, and resets it to zero.
    /// </summary>
    /// <param name="count">The number of acquired references to release and reset to zero.</param>
    public void LockAndRelease(ref int count)
    {
        if (count == 0)
        {
            return;
        }

        using (this.LockObject.EnterScope())
        {
            this.Release(ref count);
        }
    }

    /// <summary>
    /// Marks a valid owner as releasing and returns whether it has no active acquisitions.
    /// </summary>
    /// <param name="state">Receives the owner's state after the release attempt.</param>
    /// <returns>True if the owner was valid and had no acquisitions; otherwise false. A valid owner becomes Releasing in either case.</returns>
    public bool LockAndTryRelease(out GoshujinState state)
    {
        var result = false;
        using (this.LockObject.EnterScope())
        {
            if (!this.IsValid)
            {// Invalid (Releasing/Obsolete)
            }
            else
            {// Valid
                this.State = GoshujinState.Releasing;
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

    /// <summary>
    /// Marks the owner as obsolete. The caller must hold LockObject.
    /// </summary>
    public void SetObsolete()
        => this.State = GoshujinState.Obsolete;

    /// <summary>
    /// Marks the owner as releasing. The caller must hold LockObject.
    /// </summary>
    public void SetReleasing()
        => this.State = GoshujinState.Releasing;

    /// <summary>
    /// Locks the owner and marks it as releasing without waiting for acquisitions to finish.
    /// </summary>
    public void LockAndForceRelease()
    {
        using (this.LockObject.EnterScope())
        {
            this.State = GoshujinState.Releasing;
        }
    }
}
