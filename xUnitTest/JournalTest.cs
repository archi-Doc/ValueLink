// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
public partial class JournalIdentifier
{
    [Key(0)]
    public int Id0 { get; set; }

    [Key(1)]
    public int Id1 { get; set; }
}

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

[ValueLinkObject]
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass2
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [KeyAsName]
    private JournalIdentifier id;

    public JournalTestClass2()
    {
        this.id = new();
    }
}

public class JournalTest
{
    [Fact]
    public void Test1()
    {
    }
}
