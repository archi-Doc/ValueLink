## CrossLink
![Nuget](https://img.shields.io/nuget/v/CrossLink) ![Build and Test](https://github.com/archi-Doc/CrossLink/workflows/Build%20and%20Test/badge.svg)

CrossLink is a C# Library for creating and managing multiple links between objects.

It's like generic collection for objects, like ```List<T>``` for ```T```, but CrossLink is more flexible and faster than generic collections.

This document may be inaccurate. It would be greatly appreciated if anyone could make additions and corrections.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)



## Quick Start

CrossLink uses Source Generator, so the Target Framework should be .NET 5 or later.

Install CrossLink using Package Manager Console.

```
Install-Package CrossLink
```

This is a small sample code to use CrossLink.

```csharp
using System;
using System.Collections.Generic;
using CrossLink;

namespace ConsoleApp1
{
    [CrossLinkObject] // Annote a [CrossLinkObject] attribute.
    public partial class TestClass // partial class is required for source generator.
    {
        [Link(Type = LinkType.Ordered)]// Sorted link associated with id.
        private int id;// Generated property name: Id, chain name: IdChain

        [Link(Type = LinkType.Ordered)]// Sorted link associated with name.
        public string name { get; private set; } = string.Empty;// Generated property name: Id, chain name: IdChain

        [Link(Type = LinkType.Ordered)]// Sorted link associated with age.
        private int age;// Generated property name: Id, chain name: IdChain

        [Link(Type = LinkType.StackList, Name = "Stack")]// Stack (Constructor can have multiple Link attributes)
        [Link(Type = LinkType.List, Name = "List")]// List
        public TestClass(int id, string name, int age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override string ToString() => $"ID:{this.id,2}, {this.name,-5}, {this.age,2}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CrossLink Quick Start.");
            Console.WriteLine();

            var g = new TestClass.GoshujinClass(); // Create a Goshujin (Owner) instance
            new TestClass(1, "Hoge", 27).Goshujin = g; // Create TestClass and associate with the Goshujin (Owner)
            new TestClass(2, "Fuga", 15).Goshujin = g;
            new TestClass(1, "A", 7).Goshujin = g;
            new TestClass(0, "Zero", 50).Goshujin = g;

            ConsoleWriteIEnumerable("[List]", g.ListChain); // ListChain is virtually a List<TestClass>
            /* Result;  displayed in the order in which they were created.
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 15
                 ID: 1, A    ,  7
                 ID: 0, Zero , 50 */

            Console.WriteLine("ListChain[2] : "); // ListChain can be accessed by index.
            Console.WriteLine(g.ListChain[2]); // ID: 1, A    ,  7
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Sorted by Id]", g.IdChain);
            /* Sorted by Id
                 ID: 0, Zero , 50
                 ID: 1, Hoge , 27
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15 */

            ConsoleWriteIEnumerable("[Sorted by Name]", g.NameChain);
            /* Sorted by Name
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50 */

            ConsoleWriteIEnumerable("[Sorted by Age]", g.AgeChain);
            /* Sorted by Age
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50 */

            var t = g.ListChain[1];
            Console.WriteLine($"{t.Name} age {t.Age} => 95"); // Change Fuga's age to 95.
            t.Age = 95;
            ConsoleWriteIEnumerable("[Sorted by Age]", g.AgeChain);
            /* AgeChain will be updated automatically.
                 ID: 1, A    ,  7
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50
                 ID: 2, Fuga , 95 */

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            /* Stack chain  
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7
                 ID: 0, Zero , 50 */

            t = g.StackChain.Pop(); // Pop an object. Note that only StackChain is affected.
            Console.WriteLine($"{t.Name} => Pop");
            g.Remove(t);// To remove the object from other chains, you need to call Goshujin's Remove().
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            /* Zero is removed.
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7 */

            var g2 = new TestClass.GoshujinClass(); // New Goshujin
            t = g.ListChain[0];
            Console.WriteLine($"{t.Name} Goshujin => Goshujin2");
            Console.WriteLine();
            t.Goshujin = g2; // Change from Goshujin to Goshujin2.
            ConsoleWriteIEnumerable("[Goshujin]", g.ListChain);
            ConsoleWriteIEnumerable("[Goshujin2]", g2.ListChain);
            /*
             * [Goshujin]
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7
                [Goshujin2]
                 ID: 1, Hoge , 27*/

            static void ConsoleWriteIEnumerable<T>(string? header, IEnumerable<T> e)
            {
                if (header != null)
                {
                    Console.WriteLine(header);
                }

                foreach (var x in e)
                {
                    Console.WriteLine(x!.ToString());
                }

                Console.WriteLine();
            }
        }
    }
}
```



## Performance

Performance is the top priority.

Although CrossLink do a little bit complex process than generic collection classes, CrossLink is faster than generic collection classes.

This is a benchmark with generic collection ```SortedDictionary<TKey, TValue>```.
The following code creates an instance of a collection, creates H2HClass and adds to the collection in sorted order.

```csharp
var g = new SortedDictionary<int, H2HClass>();
foreach (var x in this.IntArray)
{
    g.Add(x, new H2HClass(x));
}
```

This is the CrossLink version and it does almost the same process (In fact, CrossLink is more scalable and flexible).

```csharp
var g = new H2HClass2.GoshujinClass();
foreach (var x in this.IntArray)
{
    new H2HClass2(x).Goshujin = g;
}
```

The result; CrossLink is faster than plain ```SortedDictionary<TKey, TValue>```.

| Method                     | Length |       Mean |    Error |   StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
| -------------------------- | ------ | ---------: | -------: | -------: | -----: | -----: | ----: | --------: |
| NewAndAdd_SortedDictionary | 100    | 7,209.8 ns | 53.98 ns | 77.42 ns | 1.9379 |      - |     - |    8112 B |
| NewAndAdd_CrossLink        | 100    | 4,942.6 ns | 12.28 ns | 17.99 ns | 2.7084 | 0.0076 |     - |   11328 B |

When it comes to modifying an object (remove/add), CrossLink is much faster than the collection class.

| Method                        | Length |       Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ----------------------------- | ------ | ---------: | -------: | -------: | -----: | ----: | ----: | --------: |
| RemoveAndAdd_SortedDictionary | 100    | 1,491.1 ns | 13.01 ns | 18.24 ns | 0.1335 |     - |     - |     560 B |
| RemoveAndAdd_CrossLink        | 100    |   524.1 ns |  3.76 ns |  5.63 ns | 0.1717 |     - |     - |     720 B |



## How it works

CrossLink works by adding an inner class and some properties to an existing class.

1. Add an inner class named "GoshujinClass" to the target class.
2. Create a property which corresponds to the member with Link attribute. The first letter of the property will be capitalized. For example, "id" becomes "Id". 



This is a tiny class to demonstrate how CrossLink works.

```csharp
public partial class TinyClass // partial class is required for source generator.
{
    [Link(Type = LinkType.Ordered)] // Add Link attribute to a member.
    private int id;
}
```

When building a project, CrossLink first creates an inner class called ```GoshujinClass```. ```GoshujinClass``` is the owner class for managing multiple TinyClass instances.

```csharp
public sealed class GoshujinClass
{// Goshujin-sama means an owner in Japanese.
    public void Add(TinyClass x)
    {// Add TinyClass to Goshujin.
        this.IdChain.Add(x.id, x);// IdChain
    }
    public void Remove(TinyClass x)
    {// Remove TinyClass from Goshujin.
        this.IdChain.Remove(x);
    }

    // IdChain is a collection of TinyClass that are maintained in a sorted order.
    public OrderedChain<int, TinyClass> IdChain { get; } = new(static x => ref x.IdLink);
}
```

The following code adds a member of TinyClass that holds a Goshujin instance.

```csharp
private GoshujinClass __gen_visceral_identifier__0001 = default!; // Actual Goshujin instance.

public GoshujinClass Goshujin
{
    get => this.__gen_visceral_identifier__0001; // Getter
    set
    {// Set a Goshujin instance.
        if (value != this.__gen_visceral_identifier__0001)
        {
            if (this.__gen_visceral_identifier__0001 != null)
            {// Remove TinyClass from previous Goshujin.
                this.__gen_visceral_identifier__0001.Remove(this);
            }

            this.__gen_visceral_identifier__0001 = value;// Set a new value.
            if (value != null)
            {// Add TinyClass to new Goshujin.
                this.__gen_visceral_identifier__0001.Add(this);
            }
        }
    }
}
```





## Features

### AutoNotify (INotifyPropertyChanged)