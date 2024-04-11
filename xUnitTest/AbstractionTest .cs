// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

[TinyhandUnion(0, typeof(NonAbstractTestClass))]
[TinyhandUnion(1, typeof(NonAbstractTestClass2))]
[ValueLinkObject]
public abstract partial class AbstractTestClass
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

[TinyhandObject]
public partial class NonAbstractTestClass : AbstractTestClass
{
    [Key(1)]
    // [Link(Primary = true, Type = ChainType.Ordered)]
    public string Name { get; set; } = string.Empty;

    public NonAbstractTestClass()
    {
    }

    public NonAbstractTestClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }
}

[TinyhandObject]
public partial class NonAbstractTestClass2 : AbstractTestClass
{
    [Key(1)]
    // [Link(Primary = true, Type = ChainType.Ordered)]
    public double Age { get; set; }

    public NonAbstractTestClass2()
    {
    }

    public NonAbstractTestClass2(int id, double age)
    {
        this.Id = id;
        this.Age = age;
    }
}

/*[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class GenericTestClass3<TClass>
    where TClass : AbstractTestClass
{
    [Link(Primary = true, TargetMember = "Id", Type = ChainType.Ordered)]
    public GenericTestClass3(TClass target)
    {
        this.Target = target;
    }

    private GenericTestClass3()
    {
        this.Target = default!;
    }

    [Key(0)]
    public TClass Target { get; private set; }

    public int Id => this.Target.Id;
}*/

public class AbstractionTest
{
    [Fact]
    public void Test1()
    {
        var g = new AbstractTestClass.GoshujinClass();
        g.Add(new NonAbstractTestClass(1, "A"));
        g.Add(new NonAbstractTestClass(2, "BB"));
        g.Add(new NonAbstractTestClass2(3, 33));
        var bin = TinyhandSerializer.Serialize(g);
        var g2 = TinyhandSerializer.Deserialize<AbstractTestClass.GoshujinClass>(bin)!;

        ((NonAbstractTestClass)g2.IdChain.FindFirst(1)!).Name.Is("A");
        ((NonAbstractTestClass)g2.IdChain.FindFirst(2)!).Name.Is("BB");
        ((NonAbstractTestClass2)g2.IdChain.FindFirst(3)!).Age.Is(33);
    }

    /*[Fact]
    public void Test2()
    {// ....
        var g = new GenericTestClass3<AbstractTestClass>.GoshujinClass();
        g.Add(new GenericTestClass3<NonAbstractTestClass>(new(1, "a")));
    }*/
}
