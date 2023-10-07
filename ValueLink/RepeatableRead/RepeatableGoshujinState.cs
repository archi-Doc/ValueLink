// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace ValueLink;

public struct RepeatableGoshujinState
{
    public RepeatableGoshujinState()
    {
    }

    private int count;

    public bool IsValid => Volatile.Read(ref this.count) >= 0;

    public bool CanUnload => Volatile.Read(ref this.count) == 0;

    public bool TryLock()
    {
        int value;
        do
        {
            value = this.count;
            if (value < 0)
            {
                return false;
            }
        }
        while (Interlocked.CompareExchange(ref this.count, value + 1, value) != value);
        return true;
    }

    public void Release()
    {
        Interlocked.Decrement(ref this.count);
    }

    public void ForceUnload()
    {
        Volatile.Write(ref this.count, -1);
    }
}
