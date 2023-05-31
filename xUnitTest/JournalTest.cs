// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
public readonly partial struct JournalIdentifier
{
    [Key(0)]
    public readonly int Id0;

    [Key(1)]
    public readonly int Id1;
}

[ValueLinkObject(Lock = true)]
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [KeyAsName]
    private int id;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    private JournalIdentifier identifier = default!;

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
        var tc = new JournalTestClass();
        /*using (tc.Goshujin.Lock())
        {
            tc.Goshujin.IdChain.Is
        }
            tc.IdLink.Next;*/
    }
}
