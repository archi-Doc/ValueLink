// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using ValueLink.Integrality;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SimpleIntegralityClass : IEquatableObject<SimpleIntegralityClass>
{
    public class Integrality : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly Integrality Instance = new()
        {
            MaxItems = 1000,
            RemoveIfItemNotFound = true,
        };
    }

    public SimpleIntegralityClass()
    {
    }

    public SimpleIntegralityClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    bool IEquatableObject<SimpleIntegralityClass>.ObjectEquals(SimpleIntegralityClass other)
        => this.Id == other.Id && this.Name == other.Name;
}

public class IntegralityTest
{
    [Fact]
    public void Test1()
    {
        var g = new SimpleIntegralityClass.GoshujinClass();
        g.Add(new(1, "A"));
        g.Add(new(2, "B"));
        g.Add(new(3, "C"));

        var g2 = new SimpleIntegralityClass.GoshujinClass();
        g2.GoshujinEquals(g).IsFalse();

        SimpleIntegralityClass.Integrality.Instance.IntegrateForTest(g2, g).Is(IntegralityResult.Success);
        g2.GoshujinEquals(g).IsTrue();
    }
}
