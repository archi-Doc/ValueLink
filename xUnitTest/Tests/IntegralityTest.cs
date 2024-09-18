// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using ValueLink.Integrality;
using System.Threading.Tasks;
using Tinyhand.Formatters;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SimpleIntegralityClass : IEquatableObject<SimpleIntegralityClass>
{
    public class Integrality : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly Integrality Instance10 = new()
        {
            MaxItems = 10,
            RemoveIfItemNotFound = true,
        };

        public static readonly Integrality Instance2 = new()
        {
            MaxItems = 2,
            RemoveIfItemNotFound = true,
        };
    }

    public class IntegralityNotB : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly IntegralityNotB Instance = new()
        {
            MaxItems = 10,
            RemoveIfItemNotFound = true,
        };

        public override bool Validate(GoshujinClass goshujin, SimpleIntegralityClass newItem, SimpleIntegralityClass? oldItem)
        {
            if (newItem.Name == "B")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
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

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class SerializableIntegralityClass : IEquatableObject<SerializableIntegralityClass>
{
    public SerializableIntegralityClass()
    {
    }

    public SerializableIntegralityClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    bool IEquatableObject<SerializableIntegralityClass>.ObjectEquals(SerializableIntegralityClass other)
        => this.Id == other.Id && this.Name == other.Name;
}

public static class IntegralityTestHelper
{
    public static IntegralityResult IntegrateForTest<TGoshujin, TObject>(this Integrality<TGoshujin, TObject> integrality, TGoshujin goshujin, TGoshujin target)
        where TGoshujin : class, IGoshujin, IIntegralityObject, IIntegralityGoshujin
        where TObject : class, ITinyhandSerialize<TObject>, IIntegralityObject
        => integrality.Integrate(goshujin, (x, y) => Task.FromResult(integrality.Differentiate(target, x))).Result;
}

public class IntegralityTest
{
    [Fact]
    public void Test1()
    {
        SimpleIntegralityClass.GoshujinClass g;
        SimpleIntegralityClass.GoshujinClass g2;

        g = new(); // 1, 2, 3
        g.Add(new(1, "A"));
        g.Add(new(2, "B"));
        g.Add(new(3, "C"));

        g2 = new(); // 1, 2, 3
        g2.GoshujinEquals(g).IsFalse();

        SimpleIntegralityClass.Integrality.Instance10.IntegrateForTest(g2, g).Is(IntegralityResult.Success);
        g2.GoshujinEquals(g).IsTrue();

        g2 = new(); // 1, 2
        SimpleIntegralityClass.Integrality.Instance2.IntegrateForTest(g2, g).Is(IntegralityResult.Incomplete);
        g2.IdChain.FindFirst(1).IsNotNull();
        g2.IdChain.FindFirst(2).IsNotNull();
        g2.IdChain.FindFirst(3).IsNull();
        g2.GoshujinEquals(g).IsFalse();

        g2 = new(); // 1, 3
        SimpleIntegralityClass.IntegralityNotB.Instance.IntegrateForTest(g2, g).Is(IntegralityResult.Incomplete);
        g2.IdChain.FindFirst(1).IsNotNull();
        g2.IdChain.FindFirst(2).IsNull();
        g2.IdChain.FindFirst(3).IsNotNull();
        g2.GoshujinEquals(g).IsFalse();

        g2 = new();
        g2.GoshujinEquals(g).IsFalse();

        SimpleIntegralityClass.Integrality.Instance10.IntegrateForTest(g, g2).Is(IntegralityResult.Incomplete);
    }
}
