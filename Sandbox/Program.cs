using System;
using ValueLink;
using Tinyhand;

namespace Sandbox;


[TinyhandObject(ExplicitKeyOnly = true, Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableItem
{
    public RepeatableItem()
    {
    }

    public RepeatableItem(int id)
    {
        this.Id = id;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id { private set; get; }
}

[TinyhandObject(Structual = true)]
[ValueLinkObject]
internal partial class ValueClass
{
    [Key(0, AddProperty = "Id")]
    [Link(Type = ChainType.Unordered, Primary = true, Unique = true, Accessibility = ValueLinkAccessibility.Public)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    private string name = string.Empty;

    public class Maid : ValueClass
    {
    }
}

[TinyhandObject]
public partial class GenericClass<T>
{
    [Key(0)]
    public T X { get; set; } = default!;

    [TinyhandObject]
    [ValueLinkObject]
    private partial class Item
    {
        [Key(0)]
        [Link(Primary = true, Type = ChainType.Ordered)]
        public string Name { get; set; } = string.Empty;
    }

    public GenericClass()
    {
        var item = new Item();
        TinyhandSerializer.Serialize(item);
    }
}

public partial class NestedStructClass<T, U>
    where T : struct
    where U : class
{
    // [TinyhandObject]
    [ValueLinkObject]
    private sealed partial class Item
    {
        [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
        public Item(int key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value;

        [Key(1)]
        [Link(Type = ChainType.Unordered)]
        internal int Key;
    }

    public NestedStructClass()
    {
    }

    private Item.GoshujinClass goshujin = new();
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        var tc = new GenericClass<int>();
    }
}
