// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Threading;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject(Lock = true)]
public partial record LockTestClass
{
    [Link(Primary = true, Type = ChainType.Ordered)]
    private int id;

    public LockTestClass()
    {
    }

    /*private Arc.Threading.ILockable __gen_cl_enter__()
    {
        while (true)
        {
            var lockObject = this.__gen_cl_identifier__001?.LockObject ?? __gen_cl_null_lock__;
            lockObject.Enter();
            var lockObject2 = this.__gen_cl_identifier__001?.LockObject ?? __gen_cl_null_lock__;
            if (lockObject == lockObject2 && !this.gosjujinLock)
            {
                this.gosjujinLock = true;
                return lockObject;
            }

            lockObject.Exit();
        }
    }*/

    private void __gen_cl_exit__(Arc.Threading.ILockable lockObject)
    {
        lockObject.Exit();
    }
}

public class LockTest
{
    [Fact]
    public void Test1()
    {
        var tc = new LockTestClass();

    }
}
