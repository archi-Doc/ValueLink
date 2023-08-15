// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using Tinyhand.IO;
using System;

namespace xUnitTest;

[TinyhandObject]
public readonly partial struct JournalIdentifier : IComparable<JournalIdentifier>, IEquatable<JournalIdentifier>
{
    public JournalIdentifier(int id)
    {
        this.Id0 = id;
        this.Id1 = id;
    }

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
[TinyhandObject(Journal = true)]
public partial record JournalTestClass : IEquatableObject<JournalTestClass>
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
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
[TinyhandObject(Journal = true)]
public partial record JournalTestClass2 : IEquatableObject<JournalTestClass2>
{
    public JournalTestClass2()
    {
        this.child = new();
        // ((IJournalObject)this.child).SetParent(this, 2);
    }

    public JournalTestClass2(JournalIdentifier id, string name)
    {
        this.id = id;
        this.name = name;

        this.child = new();
        // ((IJournalObject)this.child).SetParent(this, 2);
        this.child.Age = id.Id0 + 0.1d;
    }

    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    [Key(0)]
    private JournalIdentifier id;

    [Key(1)]
    [MaxLength(4)]
    private string name = string.Empty;

    [Key(2)]
    private JournalChildClass child;

    [Key(3)]
    private int id2;

    [Key(4)]
    private JournalTestClass2.GoshujinClass? children; // = new();

    public bool ObjectEquals(JournalTestClass2 other)
    {
        if (!this.id.Equals(other.id))
        {
            return false;
        }

        if (this.name != other.name)
        {
            return false;
        }

        if (!this.child.ObjectEquals(other.child))
        {
            return false;
        }

        if (this.id2 != other.id2)
        {
            return false;
        }

        if (this.children is null)
        {
            if (other.children is not null)
            {
                return false;
            }
        }
        else
        {
            if (other.children is null)
            {
                return false;
            }
            else if (!this.children.GoshujinEquals(other.children))
            {
                return false;
            }
        }

        return true;
    }
}

[TinyhandObject(Journal = true, ExplicitKeyOnly = true)]
public partial record JournalTestBase
{
    public JournalTestBase()
    {
    }

    [Key(0)]
    public int Id { get; set; }

    public JournalTestBase? Parent
    {
        get => this.parent;
        set
        {
            this.parent = value;
        }
    }

    private JournalTestBase? parent;
}

[TinyhandObject(Journal = true, ExplicitKeyOnly = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record JournalTestBase2 : JournalTestBase
{
    public JournalTestBase2()
    {
    }

    [Key(1)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id2 { get; set; }

    [Key(2)]
    public readonly int Id3;
}

/*[TinyhandObject(Journal = true, ExplicitKeyOnly = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record JournalTestBase3 : JournalTestBase2
{
    public JournalTestBase3()
    {
    }

    [Key(2)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id3 { get; set; }
}*/

[TinyhandObject(Journal = true)]
public partial record JournalChildClass : IEquatableObject<JournalChildClass>
{
    public JournalChildClass()
    {
    }

    public JournalChildClass(double age)
    {
        this.age = age;
    }

    [Key(0, AddProperty = "Age")]
    private double age;

    public bool ObjectEquals(JournalChildClass other)
        => this.age == other.age;
}

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
internal partial record StandardData
{
    public StandardData()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    private int id;

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
        cc.Journal = tester;
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

        g2.Journal = tester;
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

    [Fact]
    public void TestGoshujin2()
    {
        var tester = new JournalTester();
        var c1 = new JournalTestClass2(new(1), "one");
        var c2 = new JournalTestClass2(new(2), "two");
        var c3 = new JournalTestClass2(new(3), "3");

        var g = new JournalTestClass2.GoshujinClass { c1, c2, c3 };
        var g2 = new JournalTestClass2.GoshujinClass();

        g2.Journal = tester;
        g2.Add(new JournalTestClass2(new(1), "one"));
        g2.Add(new JournalTestClass2(new(2), "two"));
        g2.Add(new JournalTestClass2(new(3), "3"));
        g.GoshujinEquals(g2).IsTrue();

        var journal = tester.GetJournal();
        var g3 = new JournalTestClass2.GoshujinClass();
        JournalHelper.ReadJournal(g3, journal).IsTrue();
        g.GoshujinEquals(g3).IsTrue();

        g2.IdChain.FindFirst(new(1))!.Goshujin = null;
        g2.Add(new JournalTestClass2(new(4), "four"));
        g2.IdChain.FindFirst(new(3))!.Goshujin = null;

        journal = tester.GetJournal();
        g3 = new JournalTestClass2.GoshujinClass();
        JournalHelper.ReadJournal(g3, journal).IsTrue();
        g2.GoshujinEquals(g3).IsTrue();

        using (var w = g2.TryLock(new JournalIdentifier(2)))
        {
            w!.Id = new(10);
            w!.Name = "Ten";
            w!.Commit();
        }

        using (var w = g2.TryLock(new JournalIdentifier(20), TryLockMode.GetOrCreate))
        {
            w!.Name = "20";
            w!.Id = new(222);
            w!.Child.Age = 20.2d; // Caution! Changes to values are reflected immediately (it is not repeatable read).
            w!.Id2 = 2;
            w!.Commit();
        }

        journal = tester.GetJournal();
        g3 = new JournalTestClass2.GoshujinClass();
        this.ReadJournal(g3, journal).IsTrue();
        g2.GoshujinEquals(g3).IsTrue();

        using (var w = g2.TryLock(new JournalIdentifier(222)))
        {
            if (w is not null)
            {
                w.Name = "BB";
                w.Child.Age = 30.2d; // Caution! Changes to values are reflected immediately (it is not repeatable read).
                w.Commit();
            }
        }

        var r = g2.TryGet(new JournalIdentifier(20));
        if (r is not null)
        {
            g2.Remove(r);
        }

        using (var w = g2.TryLock(new JournalIdentifier(5), TryLockMode.GetOrCreate))
        {
            if (w is not null)
            {
                w.Name = "Fi";
                w.Id2 = 5;
                w.Commit();
            }
        }

        using (var w = g2.TryLock(new JournalIdentifier(4)))
        {
            if (w is not null)
            {
                w.Name = "FF";
                w.Commit();
            }
        }

        using (var w = g2.TryLock(new JournalIdentifier(4)))
        {
            if (w is not null)
            {
                w.Goshujin = null;
            }
        }

        using (var w = g2.TryLock(new JournalIdentifier(4)))
        {
            if (w is not null)
            {
                w.Goshujin = null;
                w.Commit();
            }
        }

        for (var i = 0; i < 100; i++)
        {
            using (var w = g2.TryLock(new JournalIdentifier(i), TryLockMode.GetOrCreate))
            {
                if (w is not null)
                {
                    w.Commit();
                }
            }
        }

        using (var w = g2.TryLock(new JournalIdentifier(50)))
        {
            if (w is not null)
            {
                w.Name = "50";
                w.Id2 = 50;

                using (var w2 = g2.TryLock(new JournalIdentifier(51)))
                {
                    if (w2 is not null)
                    {
                        w2.Name = "51";
                        w2.Id2 = 51;
                        w2.Commit();
                        w.Commit();
                    }
                }
            }
        }

        using (var w = g2.TryLock(new JournalIdentifier(60)))
        {
            if (w is not null)
            {
                w.Children ??= new();
                w.Children.Add(new(new(60), "six"));
                w.Commit();

                using (var w2 = w.Children.TryLock(new JournalIdentifier(10), TryLockMode.GetOrCreate))
                {
                    if (w2 is not null)
                    {
                        w2.Name = "Y";
                        w2.Commit();
                    }
                }
            }
        }

        journal = tester.GetJournal();
        g3 = new JournalTestClass2.GoshujinClass();
        this.ReadJournal(g3, journal).IsTrue();
        g2.GoshujinEquals(g3).IsTrue();
    }

    public bool ReadJournal(IJournalObject journalObject, ReadOnlyMemory<byte> data)
    {
        var reader = new TinyhandReader(data.Span);
        var success = true;

        while (reader.Consumed < data.Length)
        {
            if (!reader.TryReadRecord(out var length, out var journalType))
            {
                return false;
            }

            var fork = reader.Fork();
            try
            {
                if (journalType == JournalType.Record)
                {
                    if (journalObject.ReadRecord(ref reader))
                    {// Success
                    }
                    else
                    {// Failure
                        success = false;

                        reader = fork;
                        journalObject.ReadRecord(ref reader);
                    }
                }
                else
                {
                }
            }
            catch
            {
                success = false;
            }
            finally
            {
                reader = fork;
                reader.Advance(length);
            }
        }

        return success;
    }
}
