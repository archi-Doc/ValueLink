// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
}

public class LockTest
{
    [Fact]
    public void Test1()
    {
        var tc = new LockTestClass();

    }
}
