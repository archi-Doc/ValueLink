// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
public partial class PrivateIntClass
{
    public PrivateIntClass()
    {
    }

    [Key(0)]
    public int Id { get; private set; }
}

[TinyhandObject]
[ValueLinkObject]
public partial class PrivateIntClass2 : PrivateIntClass
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Id")]
    public PrivateIntClass2()
    {
    }

    // [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    // public int Id2 => this.Id;
}

[ValueLinkObject]
[TinyhandObject]
public partial class TestClass3
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false, Accessibility = ValueLinkAccessibility.Public)]
    [Link(Name = "Id2", Type = ChainType.Unordered)]
    [MemberNameAsKey]
    private int Id;

    [Link(Primary = true, Type = ChainType.Ordered)]
    [Link(Name = "AgeUn", Type = ChainType.Unordered)]
    [Link(Name = "AgeRev", Type = ChainType.ReverseOrdered)]
    [MemberNameAsKey]
    public int Age { get; set; }

    // [Link(Type = ChainType.Ordered)]
    public string Name { get; } = string.Empty;

    public TestClass3()
    {
    }

    public TestClass3(int id, byte age)
    {
        this.Id = id;
        this.Age = age;
    }
}

public class BasicTest2
{
    [Fact]
    public void Test1()
    {
        var g = new TestClass3.GoshujinClass();
        g.Add(new TestClass3(1, 1));
        var tc3 = new TestClass3(3, 3);
        g.Add(tc3);
        g.Add(new TestClass3(0, 0));
        g.Add(new TestClass3(5, 5));
        g.Add(new TestClass3(2, 2));

        var array = g.AgeChain.Select(a => a.Age).ToArray();
        array.SequenceEqual(new int[] { 0, 1, 2, 3, 5, }).IsTrue();

        array = g.AgeRevChain.Select(a => a.Age).ToArray();
        array.SequenceEqual(new int[] { 5, 3, 2, 1, 0, }).IsTrue();

        tc3.Goshujin = null;

        array = g.AgeChain.Select(a => a.Age).ToArray();
        array.SequenceEqual(new int[] { 0, 1, 2, 5, }).IsTrue();

        array = g.AgeRevChain.Select(a => a.Age).ToArray();
        array.SequenceEqual(new int[] { 5, 2, 1, 0, }).IsTrue();
    }
}
