using Arc.Collections;
using Tinyhand;
using ValueLink;
using ValueLink.Integrality;

namespace Playground;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SimpleIntegralityClass
{
    public SimpleIntegralityClass()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }
}

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class GenericIntegralityClass<T>
    where T : ITinyhandSerialize<T>
{
    public class Integrality : Integrality<GoshujinClass, GenericIntegralityClass<T>>
    {
        public static readonly ObjectPool<Integrality> Pool = new(
            () => new()
            {
                MaxItems = 1000,
                RemoveIfItemNotFound = false,
            },
            4);

        public override bool Validate(GoshujinClass goshujin, GenericIntegralityClass<T> newItem, GenericIntegralityClass<T>? oldItem)
        {
            return true;
        }
    }

    public GenericIntegralityClass()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class GenericIntegralityClass2<T>
    where T : ITinyhandSerialize<T>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Id2")]
    public GenericIntegralityClass2()
    {
    }

    public int Id2 => this.id2;

    [Key(1)]
    public T Value { get; set; } = default!;

    [Key(0)]
    private int id2;

    [Key(2, AddProperty = "Name")]
    private string name = string.Empty;//
}
