## CrossLink
![Nuget](https://img.shields.io/nuget/v/CrossLink) ![Build and Test](https://github.com/archi-Doc/CrossLink/workflows/Build%20and%20Test/badge.svg)

CrossLink is a C# Library for creating and managing multiple links between objects.

It's like generic collections for objects, like ```List<T>``` for ```T```, but CrossLink is more flexible and faster than generic collections.



This document may be inaccurate. It would be greatly appreciated if anyone could make additions and corrections.



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [How it works](#how-it-works)
- [Chains](#chains)
- [Features](#features)
  - [Serialization](#serialization)
  - [AutoNotify](#autonotify)



## Quick Start

CrossLink uses Source Generator, so the Target Framework should be .NET 5 or later.

First, install CrossLink using Package Manager Console.

```
Install-Package CrossLink
```

This is a sample code to use CrossLink.

```csharp
using System;
using System.Collections.Generic;
using CrossLink;

#pragma warning disable SA1300

namespace ConsoleApp1
{
    [CrossLinkObject] // Annote a CrossLinkObject attribute.
    public partial class TestClass // Partial class is required for source generator.
    {
        [Link(Type = ChainType.Ordered)] // Sorted link associated with id.
        private int id; // Generated property name: Id, chain name: IdChain
        // The generated property is for changing values and updating links.
        // The generated link is for storing information between objects, similar to a node in a collection.

        [Link(Type = ChainType.Ordered)] // Sorted link associated with name.
        public string name { get; private set; } = string.Empty; // Generated property name: Id, chain name: IdChain

        [Link(Type = ChainType.Ordered)]// Sorted link associated with age.
        private int age; // Generated property name: Id, chain name: IdChain

        [Link(Type = ChainType.StackList, Name = "Stack")] // Stack (Constructor can have multiple Link attributes)
        [Link(Type = ChainType.List, Name = "List")] // List
        public TestClass(int id, string name, int age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override string ToString() => $"ID:{this.id,2}, {this.name,-5}, {this.age,2}";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("CrossLink Quick Start.");
            Console.WriteLine();

            var g = new TestClass.GoshujinClass(); // Create a Goshujin (Owner) instance
            new TestClass(1, "Hoge", 27).Goshujin = g; // Create a TestClass and associate with the Goshujin (Owner)
            new TestClass(2, "Fuga", 15).Goshujin = g;
            new TestClass(1, "A", 7).Goshujin = g;
            new TestClass(0, "Zero", 50).Goshujin = g;

            ConsoleWriteIEnumerable("[List]", g.ListChain); // ListChain is virtually List<TestClass>
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
            t.Goshujin = null; // To remove the object from other chains, you need to set Goshujin to null.
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            /* Zero is removed.
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7 */

            var g2 = new TestClass.GoshujinClass(); // New Goshujin2
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

            // g.IdChain.Remove(t); // Exception is thrown because this object belongs to Goshujin2.
            // t.Goshujin.IdChain.Remove(t); // No exception.
            
            Console.WriteLine("[IdChain First/Next]");
            t = g.IdChain.First; // Enumerate objects using Link interface.
            while (t != null)
            {
                Console.WriteLine(t);
                t = t.IdLink.Next; // Note that Next is not a Link, but an object.
            }

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

Although CrossLink do a little bit complex process than generic collections, CrossLink works faster than generic collections.

This is a benchmark with the generic collection ```SortedDictionary<TKey, TValue>```.
The following code creates an instance of a collection, creates a H2HClass and adds to the collection in sorted order.

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

When it comes to modifying an object (remove/add), CrossLink is much faster than the generic collection.

| Method                        | Length |       Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ----------------------------- | ------ | ---------: | -------: | -------: | -----: | ----: | ----: | --------: |
| RemoveAndAdd_SortedDictionary | 100    | 1,491.1 ns | 13.01 ns | 18.24 ns | 0.1335 |     - |     - |     560 B |
| RemoveAndAdd_CrossLink        | 100    |   524.1 ns |  3.76 ns |  5.63 ns | 0.1717 |     - |     - |     720 B |



## How it works

CrossLink works by adding an inner class and some properties to the existing class. 

The actual behavior is

1. Adds an inner class named ```GoshujinClass``` to the target object.
2. Adds a property named ```Goshujin``` to the target object.
3. Creates a property which corresponds to the member with a Link attribute. The first letter of the property will be capitalized. For example, ```id``` becomes ```Id```. 
4. Creates a ```Link``` field. The name of the field will the concatenation of the property name and ```Link```. For example, ```Id``` becomes ```IdLink```.



The terms

- ```Object```: An object that stores information and is the target to be connected.
- ```Goshujin```: An owner class of the objects.  It's for storing and manipulating objects.
- ```Chain```: Chain is like a generic collection. Goshujin can have multiple Chains that manage objects in various ways.
- ```Link```: Link is like a node. An object can have multiple Links that hold information about relationships between objects.



This is a tiny class to demonstrate how CrossLink works.

```csharp
public partial class TinyClass // Partial class is required for source generator.
{
    [Link(Type = ChainType.Ordered)] // Add a Link attribute to a member.
    private int id;
}
```

When building a project, CrossLink first creates an inner class called ```GoshujinClass```. ```GoshujinClass``` is an owner class for storing and manipulating multiple ```TinyClass``` instances.

```csharp
public sealed class GoshujinClass : IGoshujin // IGoshujin is a base interface for Goshujin
{// Goshujin-sama means an owner in Japanese.
    
    public GoshujinClass()
    {
        // IdChain is a collection of TinyClass that are maintained in a sorted order.
        this.IdChain = new(this, static x => x.__gen_cl_identifier__001, static x => ref x.IdLink);
    }

    public OrderedChain<int, TinyClass> IdChain { get; }
}
```

The following code adds a field and a property that holds a ```Goshujin``` instance.

```csharp
private GoshujinClass? __gen_cl_identifier__001; // Actual Goshujin instance.

public GoshujinClass? Goshujin
{
    get => this.__gen_cl_identifier__001; // Getter
    set
    {// Set a Goshujin instance.
        if (value != this.__gen_cl_identifier__001)
        {
            if (this.__gen_cl_identifier__001 != null)
            {// Remove the TinyClass from the previous Goshujin.
                this.__gen_cl_identifier__001.IdChain.Remove(this);
            }

            this.__gen_cl_identifier__001 = value;// Set a new value.
            if (value != null)
            {// Add the TinyClass to the new Goshujin.
                value.IdChain.Add(this.id, this);
            }
        }
    }
}
```

Finally, CrossLink adds a link and a property which is used to modify the collection and change the value.

```csharp
public OrderedChain<int, TinyClass>.Link IdLink; // Link is like a Node.

public int Id
{// Property "Id" is created from a member "id".
    get => this.id;
    set
    {
        if (value != this.id)
        {
            this.id = value;
            // IdChain will be updated when the value is changed.
            this.Goshujin.IdChain.Add(this.id, this);
        }
    }
}
```



## Chains

Chain is like a generic collection. CrossLink provides several kinds of chains.

| Name                  | Structure   | Access | Add      | Remove   | Search   | Sort       | Enum.    |
| --------------------- | ----------- | ------ | -------- | -------- | -------- | ---------- | -------- |
| ```ListChain```       | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |
| ```LinkedListChain``` | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```QueueListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```StackListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```OrderedChain```    | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```UnorderedChain```  | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |



## Features

### Serialization

Serializing multiple linked objects is a complicated task. However, with [Tinyhand](https://github.com/archi-Doc/Tinyhand), you can easily serialize/deserialize objects.

All you need to do is install ```Tinyhand``` package and add a ```TinyhandObject``` attribute and ```Key``` attributes to the existing object.

```
Install-Package Tinyhand
```

```csharp
[CrossLinkObject]
[TinyhandObject] // Add a TinyhandObject attribute to use TinyhandSerializer.
public partial class SerializeClass
{
    [Link(Type = ChainType.Ordered, Primary = true)] // Set primary link that is guaranteed to holds all objects in the collection in order to maximize the performance of serialization.
    [Key(0)] // Add a Key attribute to specify the key for serialization as a number or string.
    private int id;

    [Link(Type = ChainType.Ordered)]
    [Key(1)]
    private string name = default!;

    public SerializeClass()
    {// Default constructor is required for Tinyhand.
    }

    public SerializeClass(int id, string name)
    {
        this.id = id;
        this.name = name;
    }
}
```

Test code:

```csharp
var g = new SerializeClass.GoshujinClass(); // Create a new Goshujin.
new SerializeClass(1, "Hoge").Goshujin = g; // Add an object.
new SerializeClass(2, "Fuga").Goshujin = g;

var st = TinyhandSerializer.SerializeToString(g); // Serialize the Goshujin to string.
var g2 = TinyhandSerializer.Deserialize<SerializeClass.GoshujinClass>(TinyhandSerializer.Serialize(g)); // Serialize to a byte array and deserialize it.
```



### AutoNotify

By adding a ```Link``` attribute and setting ```AutoNotify``` to true, CrossLink can implement the `INotifyPropertyChanged` pattern automatically.

```csharp
[CrossLinkObject]
public partial class AutoNotifyClass
{
    [Link(AutoNotify = true)] // Set AutoNotify to true.
    private int id;

    public void Reset()
    {
        this.SetProperty(ref this.id, 0); // Change the value manually and invoke PropertyChanged.
    }
}
```

Test code:

```csharp
var c = new AutoNotifyClass();
c.PropertyChanged += (s, e) => { Console.WriteLine($"Id changed: {((AutoNotifyClass)s!).Id}"); };
c.Id = 1; // Change the value and automatically invoke PropertyChange.
c.Reset(); // Reset the value.
```

Generated code:

```csharp
public partial class AutoNotifyClass : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }
        
        storage = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        return true;
    }

    public int Id
    {
        get => this.id;
        set
        {
            if (value != this.id)
            {
                this.id = value;
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Id"));
            }
        }
    }
}
```



### AutoLink

By default, CrossLink will automatically link the object when a goshujin is set or changed.

You can change this behavior by setting AutoLink to false.

 ```csharp
[CrossLinkObject]
public partial class ManualLinkClass
{
    [Link(Type = ChainType.Ordered, AutoLink = false)] // Set AutoLink to false.
    private int id;

    public ManualLinkClass(int id)
    {
        this.id = id;
    }

    public static void Test()
    {
        var g = new ManualLinkClass.GoshujinClass();

        var c = new ManualLinkClass(1);
        c.Goshujin = g;
        Debug.Assert(g.IdChain.Count == 0, "Chain is empty.");

        g.IdChain.Add(c.id, c); // Link the object manually.
        Debug.Assert(g.IdChain.Count == 1, "Object is linked.");
    }
}
 ```

