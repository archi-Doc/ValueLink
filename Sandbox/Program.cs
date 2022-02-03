using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ValueLink;
using Tinyhand;

namespace Sandbox;

[TinyhandObject]
public partial class GenericClass<T>
{
    [Key(0)]
    public T X { get; set; } = default!;
}

public partial class NestedStructClass<T, U>
    where T : struct
    where U : class
{
    [TinyhandObject]
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
    }
}
