using System;
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
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    bool IEquatableObject<SimpleIntegralityClass>.ObjectEquals(SimpleIntegralityClass other)
        => this.Id == other.Id && this.Name == other.Name;
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
