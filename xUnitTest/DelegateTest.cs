// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
[TinyhandObject]
public partial class DelegateTestClass
{
    [Link(Primary = true, Type = ChainType.Ordered, NoValue = true, Accessibility = ValueLinkAccessibility.Public)]
    [KeyAsName]
    private int Id;

    [Link(Type = ChainType.Ordered)]
    [KeyAsName]
    public int Age { get; set; }

    [KeyAsName]
    public string Name { get; set; } = string.Empty;

    [Link(Name = "StartingWithA", Type = ChainType.LinkedList, PredicateLink = Predicate)]
    public DelegateTestClass()
    {
    }

    public DelegateTestClass(int id, int age, string name)
    {
        this.Id = id;
        this.Age = age;
        this.Name = name;
    }

    private static bool Predicate(DelegateTestClass x)
        => x.Age >= 20;
}

public class DelegateTest
{
    [Fact]
    public void Test1()
    {
        var g = new DelegateTestClass.GoshujinClass();
        g.Add(new DelegateTestClass(3, 20, "ABC"));
        g.Add(new DelegateTestClass(1, 3, "D"));
        g.Add(new DelegateTestClass(2, 10, "E"));

        var bytes = TinyhandSerializer.Serialize(g);
        var g2 = TinyhandSerializer.Deserialize<DelegateTestClass.GoshujinClass>(bytes);
        g.IsStructuralEqual(g2);
    }
}
