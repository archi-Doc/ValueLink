// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using Tinyhand.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace xUnitTest;

[TinyhandObject]
public readonly partial struct JournalIdentifier : IComparable<JournalIdentifier>, IEquatable<JournalIdentifier>
{
    [Key(0)]
    public readonly int Id0;

    [Key(1)]
    public readonly int Id1;

    public int CompareTo(JournalIdentifier other)
    {
        if (this.Id0 > other.Id0)
        {
            return 1;
        }
        else if (this.Id0 < other.Id0)
        {
            return -1;
        }

        if (this.Id1 > other.Id1)
        {
            return 1;
        }
        else if (this.Id1 < other.Id1)
        {
            return -1;
        }

        return 0;
    }

    public bool Equals(JournalIdentifier other)
        => this.Id0 == other.Id0 && this.Id1 == other.Id1;
}

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass : IEquatableObject<JournalTestClass>, IEquatableGoshujin<JournalTestClass.GoshujinClass>
{
    public JournalTestClass()
    {
    }

    public JournalTestClass(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    [Key("id", AddProperty = "Id")]
    private int id;

    [Link(Type = ChainType.Ordered)]
    [Key("identifier", AddProperty = "Identifier")]
    private JournalIdentifier identifier = default!;

    [Key("name", AddProperty = "Name")]
    private string name = string.Empty;

    public bool ObjectEquals(JournalTestClass other)
         => this.id == other.id && this.name == other.name && this.identifier.Equals(other.identifier);

    bool ValueLink.IEquatableGoshujin<xUnitTest.JournalTestClass.GoshujinClass>.GoshujinEquals(xUnitTest.JournalTestClass.GoshujinClass other)
    {
        return true;
    }
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass2
{
    public JournalTestClass2()
    {
    }

    public JournalTestClass2(JournalIdentifier id, string name)
    {
        this.id = id;
        this.name = name;
    }

    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    [Key(0)]
    private JournalIdentifier id;

    [Key(1)]
    private string name = string.Empty;
}

public class JournalTest
{
    [Fact]
    public void Test1()
    {
        var tester = new JournalTester();
        var c = new JournalTestClass(1, "one");

        var cc = new JournalTestClass();
        cc.Crystal = tester;
        cc.Id = c.Id;
        cc.Name = c.Name;

        var journal = tester.GetJournal();
        var c2 = new JournalTestClass();
        JournalHelper.ReadJournal(c2, journal).IsTrue();

        c2.IsStructuralEqual(c);
    }

    [Fact]
    public void TestGoshujin()
    {
        var tester = new JournalTester();
        var c1 = new JournalTestClass(1, "one");
        var c2 = new JournalTestClass(2, "two");
        var c3 = new JournalTestClass(3, "3");

        var g = new JournalTestClass.GoshujinClass { c1, c2, c3 };
        var g2 = new JournalTestClass.GoshujinClass();

        g2.Crystal = tester;
        g2.Add(new JournalTestClass(1, "one"));
        g2.Add(new JournalTestClass(2, "two"));
        g2.Add(new JournalTestClass(3, "3"));
        g.GoshujinEquals(g2).IsTrue();

        var journal = tester.GetJournal();
        var g3 = new JournalTestClass.GoshujinClass();
        JournalHelper.ReadJournal(g3, journal).IsTrue();
        g.GoshujinEquals(g3).IsTrue();

        g2.IdChain.FindFirst(1)!.Goshujin = null;
        g2.Add(new JournalTestClass(4, "four"));
        g2.IdChain.FindFirst(3)!.Goshujin = null;

        journal = tester.GetJournal();
        g3 = new JournalTestClass.GoshujinClass();
        JournalHelper.ReadJournal(g3, journal).IsTrue();
        g2.GoshujinEquals(g3).IsTrue();
    }
}
