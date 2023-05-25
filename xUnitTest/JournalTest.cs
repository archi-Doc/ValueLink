// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [KeyAsName]
    private int id;

    public JournalTestClass()
    {
    }
}

public class JournalTest
{
    [Fact]
    public void Test1()
    {
    }
}
