using System;
using Tinyhand;
using ValueLink;

namespace Playground;

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

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class GenericTestClass<TClass>
    where TClass : AbstractTestClass
{
    [Link(Primary = true, TargetMember = "Id", Type = ChainType.Ordered)]
    public GenericTestClass(TClass target)
    {
        this.Target = target;
    }

    private GenericTestClass()
    {
        this.Target = default!;
    }

    [Key(0)]
    public TClass Target { get; private set; }

    public int Id => this.Target.Id;
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var g = new AbstractTestClass.GoshujinClass();
        g.Add(new NonAbstractTestClass(1, "A"));
        g.Add(new NonAbstractTestClass(2, "BB"));
        g.Add(new NonAbstractTestClass2(3, 33));
        var bin = TinyhandSerializer.Serialize(g);
        var g2 = TinyhandSerializer.Deserialize<AbstractTestClass.GoshujinClass>(bin);
    }
}
