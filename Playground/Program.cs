using System;
using System.Collections.Generic;
using Tinyhand;
using ValueLink;
using ValueLink.Integrality;

namespace Playground;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class SimpleIntegralityClass : IEquatableObject<SimpleIntegralityClass>
{
    public class Integrality : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 100,
            RemoveIfItemNotFound = true
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
    public partial int Id { get; private set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [Link(Type = ChainType.Ordered)]
    public partial int Age { get; private set; }

    bool IEquatableObject<SimpleIntegralityClass>.ObjectEquals(SimpleIntegralityClass other)
        => this.Id == other.Id && this.Name == other.Name;
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World");
    }
}
