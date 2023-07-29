// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableEmpty
{
    public RepeatableEmpty()
    {
    }
}

public class IsolationTestEmpty
{
    [Fact]
    public void Test1()
    {// RepeatableRead
        var g = new RepeatableEmpty.GoshujinClass();
        var c = new RepeatableEmpty();
        var c2 = g.Add(c);
        var c3 = g.Add(c);

        c.State.Is(RepeatableObjectState.Obsolete);
        c2!.State.Is(RepeatableObjectState.Valid);
        c3.IsNull();
    }
}
