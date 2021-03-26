## CrossLink
![Nuget](https://img.shields.io/nuget/v/CrossLink) ![Build and Test](https://github.com/archi-Doc/CrossLink/workflows/Build%20and%20Test/badge.svg)

CrossLink is a C# Library for creating and managing multiple links between objects.

Work in progress



## Quick Start

CrossLink uses Source Generator, so the Target Framework should be .NET 5 or later.

Install CrossLink using Package Manager Console.

```
Install-Package CrossLink
```

This is a small sample code to use CrossLink.

```csharp
[CrossLinkObject]
    public partial class TestClass
    {
        [Link(Type = LinkType.Ordered)]
        private int id;

        [Link(Type = LinkType.Ordered)]
        public string name { get; private set; } = string.Empty;

        [Link(Type = LinkType.Ordered)]
        public int age { get; private set; }

        [Link(Type = LinkType.Ordered)]
        private double height;

        [Link(Type = LinkType.StackList, Name = "Stack")]
        public TestClass(int id, string name, int age, double height)
        {
            this.id = id;
            this.name = name;
            this.age = age;
            this.height = height;
        }

        public override string ToString() => $"ID:{this.id, 2}, {this.name, -5}, Age:{this.age, 3}, Height:{this.height:F2}";
    }
```



## Performance

Performance is my first priority.
Although CrossLink do a little bit complex process than a simple collection class, CrossLink is faster than a collection class.

This is a benchmark with generic collection ```SortedDictionary<TKey, TValue>```.
The following code creates an instance of a collection, creates H2HClass and adds to the collection in sorted order.

```csharp
var g = new SortedDictionary<int, H2HClass>();
foreach (var x in this.IntArray)
{
    g.Add(x, new H2HClass(x));
}
```

This is the CrossLink version and it does almost the same process.

```csharp
var g = new H2HClass2.GoshujinClass();
foreach (var x in this.IntArray)
{
    new H2HClass2(x).Goshujin = g;
}
```

The result; CrossLink is faster than plain ```SortedDictionary<TKey, TValue>```.



When it comes to modifying an object (remove/add), CrossLink is much faster than the collection class.



