// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using Tinyhand.IO;
using System;
using System.Runtime.CompilerServices;

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
[TinyhandObject(Journaling = true)]
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
[TinyhandObject(Journaling = true)]
public partial record JournalTestClass2 : IEquatableObject<JournalTestClass2>
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

    public bool ObjectEquals(JournalTestClass2 other)
         => this.id.Equals(other.id) && this.name == other.name;
}

public interface IJournalObject
{
    ITinyhandCrystal? Crystal { get; }

    uint Plane { get; }

    IJournalObject? Parent { get; }

    int IntKey { get; protected set; }

    /*public void AddChild(IJournalObject child, int intKey)
    {
        child.Parent = this;
        child.IntKey = intKey;
    }*/

    public bool TryGetJournalWriter(out TinyhandWriter writer)
    {
        if (this.Crystal is null)
        {
            writer = default;
            return false;
        }

        this.Crystal.TryGetJournalWriter(JournalType.Record, this.Plane, out writer);
        if (this.Parent == null)
        {
            return true;
        }
        else
        {
            var p2 = this.Parent.Parent;
            if (p2 is null)
            {
                writer.Write_Key();
                if (this.IntKey >= 0)
                {
                    writer.Write(this.IntKey);
                }
                else
                {
                    this.WriteLocator(ref writer);
                }

                return true;
            }
            else
            {
                var p3 = p2.Parent;
                if (p3 is null)
                {
                    writer.Write_Key();
                    if (this.Parent.IntKey >= 0)
                    {
                        writer.Write(this.Parent.IntKey);
                    }
                    else
                    {
                        this.Parent.WriteLocator(ref writer);
                    }

                    writer.Write_Key();
                    if (this.IntKey >= 0)
                    {
                        writer.Write(this.IntKey);
                    }
                    else
                    {
                        this.WriteLocator(ref writer);
                    }

                    return true;
                }
                else
                {
                    var p4 = p3.Parent;
                    if (p4 is null)
                    {
                        writer.Write_Key();
                        if (p2.IntKey >= 0)
                        {
                            writer.Write(p2.IntKey);
                        }
                        else
                        {
                            p2.WriteLocator(ref writer);
                        }

                        writer.Write_Key();
                        if (this.Parent.IntKey >= 0)
                        {
                            writer.Write(this.Parent.IntKey);
                        }
                        else
                        {
                            this.Parent.WriteLocator(ref writer);
                        }

                        writer.Write_Key();
                        if (this.IntKey >= 0)
                        {
                            writer.Write(this.IntKey);
                        }
                        else
                        {
                            this.WriteLocator(ref writer);
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void WriteLocator(ref TinyhandWriter writer)
    {
    }
}

public class JournalTest
{
    private void Design()
    {
        var j = (IJournalObject)default!;
        if (j.TryGetJournalWriter())
        {

        }
    }

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

    [Fact]
    public void TestGoshujin2()
    {
        var tester = new JournalTester();
        var c1 = new JournalTestClass2(new(1), "one");
        var c2 = new JournalTestClass2(new(2), "two");
        var c3 = new JournalTestClass2(new(3), "3");

        var g = new JournalTestClass2.GoshujinClass { c1, c2, c3 };
        var g2 = new JournalTestClass2.GoshujinClass();

        g2.Crystal = tester;
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

        /*using (var w = g2.TryLock(new JournalIdentifier(2)))
        {
            w!.Id = new(10);
            w!.Name = "Ten";
            w!.Commit();
        }*/

        using (var w = g2.TryLock(new JournalIdentifier(20), TryLockMode.GetOrCreate))
        {
            w!.Name = "20";
            w!.Commit();
        }

        journal = tester.GetJournal();
        g3 = new JournalTestClass2.GoshujinClass();
        this.ReadJournal(g3, journal).IsTrue();
        g2.GoshujinEquals(g3).IsTrue();
    }

    public bool ReadJournal(ITinyhandJournal journalObject, ReadOnlyMemory<byte> data)
    {
        var reader = new TinyhandReader(data.Span);
        var success = true;

        while (reader.Consumed < data.Length)
        {
            if (!reader.TryReadRecord(out var length, out var journalType, out var plane))
            {
                return false;
            }

            var fork = reader.Fork();
            try
            {
                if (journalType == JournalType.Record &&
                    journalObject.CurrentPlane == plane)
                {
                    if (journalObject.ReadRecord(ref reader))
                    {// Success
                    }
                    else
                    {// Failure
                        success = false;
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
