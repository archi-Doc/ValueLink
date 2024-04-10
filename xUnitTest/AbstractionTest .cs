// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject]
public partial class AbstractTestClass
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { get; set; }

    public AbstractTestClass()
    {
    }

    public AbstractTestClass(int id)
    {
        this.Id = id;
    }
}

public class AbstractionTest
{
    [Fact]
    public void Test1()
    {
    }
}
