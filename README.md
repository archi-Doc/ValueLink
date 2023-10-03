## ValueLink
![Nuget](https://img.shields.io/nuget/v/ValueLink) ![Build and Test](https://github.com/archi-Doc/ValueLink/workflows/Build%20and%20Test/badge.svg)

ValueLink is a C# Library for creating and managing multiple links between objects.

It's like generic collections for objects, like ```List<T>``` for ```T```, but ValueLink is more flexible and faster than generic collections.



This document may be inaccurate. It would be greatly appreciated if anyone could make additions and corrections.

日本語ドキュメントは[こちら](/doc/README.jp.md)



## Table of Contents

- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Performance](#performance)
- [How it works](#how-it-works)
- [Chains](#chains)
- [Features](#features)
  - [Serialization](#serialization)
  - [Isolation level](#isolation-level)
  - [Additional methods](#additional-methods)
  - [TargetMember](#targetmember)
  - [AutoNotify](#autonotify)
  - [AutoLink](#autolink)
  - [ObservableCollection](#observablecollection)



## Requirements

**Visual Studio 2022** or later for Source Generator V2.

**C# 9.0** or later for generated codes.

**.NET 5** or later target framework.



## Quick Start

First, install ValueLink using Package Manager Console.

```
Install-Package ValueLink
```

This is a sample code to use ValueLink.

```csharp
using System;
using System.Collections.Generic;
using ValueLink;

namespace ConsoleApp1;

[ValueLinkObject] // Annote a ValueLinkObject attribute.
public partial class TestClass // Partial class is required for source generator.
{
    [Link(Type = ChainType.Ordered)] // Sorted link associated with id.
    private int id; // Generated value name: IdValue (Name + Value), chain name: IdChain (Name + Chain)
    // Generated value is for changing values and updating links.
    // Generated link is for storing information between objects, similar to a node in a collection.

    [Link(Type = ChainType.Ordered)] // Sorted link associated with name.
    public string Name { get; private set; } = string.Empty; // Generated property name: NameValue, chain name: NameChain

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)] // Sorted link associated with age.
    [Link(Name = "AgeRev", Type = ChainType.ReverseOrdered)] // Specify a different name for the target in order to set up multiple links.
    private int age; // Generated property name: AgeValue, chain name: AgeChain

    [Link(Type = ChainType.StackList, Name = "Stack")] // Stack
    [Link(Type = ChainType.List, Name = "List")] // List
    public TestClass(int id, string name, int age)
    {
        this.id = id;
        this.Name = name;
        this.age = age;
    }

    public override string ToString() => $"ID:{this.id,2}, {this.Name,-5}, {this.age,2}";
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("ValueLink Quick Start.");
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

        ConsoleWriteIEnumerable("[Sorted by Age in reverse order]", g.AgeRevChain);
        /* Sorted by Age
             ID: 0, Zero , 50
             ID: 1, Hoge , 27
             ID: 2, Fuga , 15
             ID: 1, A    ,  7
              */

        var t = g.ListChain[1];
        Console.WriteLine($"{t.NameValue} age {t.AgeValue} => 95"); // Change Fuga's age to 95.
        t.AgeValue = 95;
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
        Console.WriteLine($"{t.NameValue} => Pop");
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

        Console.WriteLine();
        Console.WriteLine("Goshujin.Remove");
        g.Remove(g.ListChain[0]); // You can use Remove() instead of 'g.ListChain[0].Goshujin = null;'
        ConsoleWriteIEnumerable("[Goshujin]", g.ListChain);

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
```



## Performance

Performance is the top priority.

Although ValueLink do a little bit complex process than generic collections, ValueLink works faster than generic collections.

This is a benchmark with the generic collection ```SortedDictionary<TKey, TValue>```.
The following code creates an instance of a collection, creates a H2HClass and adds to the collection in sorted order.

```csharp
var g = new SortedDictionary<int, H2HClass>();
foreach (var x in this.IntArray)
{
    g.Add(x, new H2HClass(x));
}
```

This is the ValueLink version and it does almost the same process (In fact, ValueLink is more scalable and flexible).

```csharp
var g = new H2HClass2.GoshujinClass();
foreach (var x in this.IntArray)
{
    new H2HClass2(x).Goshujin = g;
}
```

The result; ValueLink is faster than plain ```SortedDictionary<TKey, TValue>```.

| Method                     | Length |       Mean |    Error |   StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
| -------------------------- | ------ | ---------: | -------: | -------: | -----: | -----: | ----: | --------: |
| NewAndAdd_SortedDictionary | 100    | 7,209.8 ns | 53.98 ns | 77.42 ns | 1.9379 |      - |     - |    8112 B |
| NewAndAdd_ValueLink        | 100    | 4,942.6 ns | 12.28 ns | 17.99 ns | 2.7084 | 0.0076 |     - |   11328 B |

When it comes to modifying an object (remove/add), ValueLink is much faster than the generic collection.

| Method                        | Length |       Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ----------------------------- | ------ | ---------: | -------: | -------: | -----: | ----: | ----: | --------: |
| RemoveAndAdd_SortedDictionary | 100    | 1,491.1 ns | 13.01 ns | 18.24 ns | 0.1335 |     - |     - |     560 B |
| RemoveAndAdd_ValueLink        | 100    |   524.1 ns |  3.76 ns |  5.63 ns | 0.1717 |     - |     - |     720 B |



## How it works

ValueLink works by adding an inner class and some properties to the existing class. 

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



This is a tiny class to demonstrate how ValueLink works.

```csharp
public partial class TinyClass // Partial class is required for source generator.
{
    [Link(Type = ChainType.Ordered)] // Add a Link attribute to a member.
    private int Id;
}
```

When building a project, ValueLink first creates an inner class called ```GoshujinClass```. ```GoshujinClass``` is an owner class for storing and manipulating multiple ```TinyClass``` instances.

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
                value.IdChain.Add(this.Id, this);
            }
        }
    }
}
```

Finally, ValueLink adds a link and a property which is used to modify the collection and change the value.

```csharp
public OrderedChain<int, TinyClass>.Link IdLink; // Link is like a Node.

public int IdValue
{// Property "IdValue" is created from a member "Id".
    get => this.Id;
    set
    {
        if (value != this.Id)
        {
            this.Id = value;
            // IdChain will be updated when the value is changed.
            this.Goshujin.IdChain.Add(this.Id, this);
        }
    }
}
```



## Chains

Chain is like a generic collection. `Goshujin` can have multiple chains corresponding to the Link attributes.

ValueLink provides several kinds of chains.

| Name                  | Structure   | Access | Add      | Remove   | Search   | Sort       | Enum.    |
| --------------------- | ----------- | ------ | -------- | -------- | -------- | ---------- | -------- |
| ```ListChain```       | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |
| ```LinkedListChain``` | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```QueueListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```StackListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```OrderedChain```    | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `ReverseOrderedChain` | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```UnorderedChain```  | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| ```ObservableChain``` | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |

If you want a new chain to be implemented, please let me know with a GitHub issue.



## Link

Link is like a node. An object can have multiple Links that hold information about relationships between objects.

Each link corresponds to a chain.



## Features

### Serialization

Serializing multiple linked objects is a complicated task. However, with [Tinyhand](https://github.com/archi-Doc/Tinyhand), you can easily serialize/deserialize objects.

All you need to do is install ```Tinyhand``` package and add a ```TinyhandObject``` attribute and ```Key``` attributes to the existing object.

```
Install-Package Tinyhand
```

```csharp
[ValueLinkObject]
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



### Isolation level

ValueLink offers several different isolation levels.



#### IsolationLevel.None

There is no additional code generated for isolation



#### IsolationLevel.Serializable

For lock-based concurrency control, the following code is added to the `Goshujin` class.

Please lock the `SyncObject` on the user side to perform exclusive operations.

```csharp
public object SyncObject { get; }
```

```csharp
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial record SerializableRoom
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; set; }

    public SerializableRoom(int roomId)
    {
    }
}
```



#### IsolationLevel.RepeatableRead

Unlike the above-mentioned Isolation levels, a lot of code is added.

Essentially, Objects become immutable, allowing for arbitrary reads. To write, you need to retrieve the object by calling `TryLock()` from the `Goshujin` class and then invoke `Commit()`.

```csharp
// An example of an object with the IsolationLevel set to RepeatableRead.
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableClass
{// Record class is required for IsolationLevel.RepeatableRead.
    public RepeatableClass()
    {// Default constructor is required.
    }

    public RepeatableClass(int id)
    {
        this.Id = id;
    }

    // A unique link is required for IsolationLevel.RepeatableRead, and a primary link is preferred for TinyhandSerializer.
    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id { get; private set; }

    [Key(1)]
    public string Name { get; private set; } = string.Empty;

    [Key(2)]
    public List<int> IntList { get; private set; } = new();

    public override string ToString()
        => $"Id: {this.Id.ToString()}, Name: {this.Name}";

    public static void Test()
    {
        var g = new RepeatableClass.GoshujinClass(); // Create a goshujin.

        g.Add(new RepeatableClass(0)); // Adds an object with id 0.

        using (var w = g.TryLock(1, TryLockMode.Create))
        {// Alternative: adds an object with id 1.
            w?.Commit(); // Commit the change.
        }

        var r0 = g.TryGet(0);
        Console.WriteLine(r0?.ToString()); // Id: 0, Name:
        Console.WriteLine();

        using (var w = g.TryLock(0))
        {
            if (w is not null)
            {
                w.Name = "Zero";
                w.Commit();
            }
        }
    }
}
```



### Additional methods

By adding methods within the class, you can determine whether to link or not, and add code to perform actions after the link has been added or removed.

```csharp
[ValueLinkObject]
public partial class AdditionalMethodClass
{
    public static int TotalAge;

    [Link(Type = ChainType.Ordered)]
    private int age;

    protected bool AgeLinkPredicate()
    {// bool Name+Link+Predicate(): Determines whether to add the object to the chain or not.
        return this.age >= 20;
    }

    protected void AgeLinkAdded()
    {// void Name+Link+Added(): Performs post-processing after the object has been added to the chain.
        TotalAge += this.age;
    }

    protected void AgeLinkRemoved()
    {// void Name+Link+Removed(): Performs post-processing after the object has been removed from the chain.
        TotalAge -= this.age;
    }
}
```



### TargetMember

If you want to create multiple goshujins from a single class, use `TargetMember` property.

```csharp
public class BaseClass
{// Base class is not ValueLinkObject.
    protected int id;

    protected string name = string.Empty;
}

[ValueLinkObject]
public partial class DerivedClass : BaseClass
{
    // Add Link attribute to constructor and set TargetMember.
    [Link(TargetMember = nameof(id), Type = ChainType.Ordered)]
    [Link(TargetMember = nameof(name), Type = ChainType.Ordered)]
    public DerivedClass()
    {
    }
}

[ValueLinkObject]
public partial class DerivedClass2 : BaseClass
{
    // Multiple ValueLinkObject can be created from the same base class.
    [Link(TargetMember = nameof(id), Type = ChainType.Unordered)]
    [Link(TargetMember = nameof(name), Type = ChainType.ReverseOrdered)]
    public DerivedClass2()
    {
    }
}

/*[ValueLinkObject] // Error! Derivation from other ValueLink objects is not supported.
public partial class DerivedClass3 : DerivedClass
{
    [Link(Type = ChainType.Ordered)]
    protected string name2 = string.Empty;
}*/

```



### AutoNotify

By adding a ```Link``` attribute and setting ```AutoNotify``` to true, ValueLink can implement the `INotifyPropertyChanged` pattern automatically.

```csharp
[ValueLinkObject]
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
c.PropertyChanged += (s, e) => { Console.WriteLine($"Id changed: {((AutoNotifyClass)s!).idValue}"); };
c.idValue = 1; // Change the value and automatically invoke PropertyChange.
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

    public int idValue
    {
        get => this.id;
        set
        {
            if (value != this.id)
            {
                this.id = value;
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("idValue"));
            }
        }
    }
}
```



### AutoLink

By default, ValueLink will automatically link the object when a goshujin is set or changed.

You can change this behavior by setting AutoLink to false.

 ```csharp
[ValueLinkObject]
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
        Debug.Assert(g.idChain.Count == 0, "Chain is empty.");

        g.IdChain.Add(c.id, c); // Link the object manually.
        Debug.Assert(g.idChain.Count == 1, "Object is linked.");
    }
}
 ```



### ObservableCollection

You can make the collection available for binding by adding ```ObservableChain```.

```ObservableChain``` is actually a wrapper class of ```ObservableCollection<T>```.

```csharp
[ValueLinkObject]
public partial class ObservableClass
{
    [Link(Type = ChainType.Ordered, AutoNotify = true)]
    private int Id { get; set; }

    [Link(Type = ChainType.Observable, Name = "Observable")]
    public ObservableClass(int id)
    {
        this.Id = id;
    }
}
```

Test code:

```csharp
var g = new ObservableClass.GoshujinClass();
ListView.ItemSource = g.ObservableChain;// You can use ObservableChain as ObservableCollection.
new ObservableClass(1).Goshujin = g;// ListView will be updated.
```

