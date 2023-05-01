// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1401 // Fields should be private

namespace ConsoleApp1;

[ValueLinkObject]
public partial class TinyClass
{// Tiny class to demonstrate how ValueLink works.
    [Link(Type = ChainType.Ordered)]
    private int id;

    public static void Test()
    {
        var g = new TinyClass.GoshujinClass();
        new TinyClass().Goshujin = g;
    }
}

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

    public static void Test()
    {
        var g = new SerializeClass.GoshujinClass(); // Create a new Goshujin.
        new SerializeClass(1, "Hoge").Goshujin = g; // Add an object.
        new SerializeClass(2, "Fuga").Goshujin = g;

        var array = g.ToArray();

        var st = TinyhandSerializer.SerializeToString(g); // Serialize to string.
        var g2 = TinyhandSerializer.Deserialize<SerializeClass.GoshujinClass>(TinyhandSerializer.Serialize(g)); // Serialize to a byte array and deserialize it.
    }
}

[ValueLinkObject]
public partial class AutoNotifyClass
{
    [Link(AutoNotify = true)] // Set AutoNotify to true.
    private int id;

    public void Reset()
    {
        this.SetProperty(ref this.id, 0); // Change the value manually and invoke PropertyChanged.
    }

    public static void Test()
    {
        var c = new AutoNotifyClass();
        c.PropertyChanged += (s, e) => { Console.WriteLine($"Id changed: {((AutoNotifyClass)s!).IdValue}"); };

        c.IdValue = 1; // Change the value and automatically invoke PropertyChange.
        c.Reset(); // Reset the value.
    }
}

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
        Debug.Assert(g.IdChain.Count == 0, "Chain is empty.");

        g.IdChain.Add(c.id, c); // Link the object manually.
        Debug.Assert(g.IdChain.Count == 1, "Object is linked.");
    }
}

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
