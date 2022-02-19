// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    [Link(Type = ChainType.List, Name = "List", Primary = true)] // List (Primary link is a link type which is guaranteed to holds all objects in the collection)
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

        ConsoleWriteIEnumerable("[Direct]", g); // You can enumerate objects directly if a primary link is specified (ListChain).

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
