﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using CrossLink;

#pragma warning disable SA1300

namespace ConsoleApp1
{
    [CrossLinkObject] // Annote a [CrossLinkObject] attribute.
    public partial class TestClass // partial class is required for source generator.
    {
        [Link(Type = LinkType.Ordered)] // Sorted link associated with id.
        private int id; // Generated property name: Id, chain name: IdChain
        // The generated property is for changing values and updating links.
        // The generated link is for storing information between objects, similar to a node in a collection.

        [Link(Type = LinkType.Ordered)] // Sorted link associated with name.
        public string name { get; private set; } = string.Empty; // Generated property name: Id, chain name: IdChain

        [Link(Type = LinkType.Ordered)]// Sorted link associated with age.
        private int age; // Generated property name: Id, chain name: IdChain

        [Link(Type = LinkType.StackList, Name = "Stack")] // Stack (Constructor can have multiple Link attributes)
        [Link(Type = LinkType.List, Name = "List")] // List
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
