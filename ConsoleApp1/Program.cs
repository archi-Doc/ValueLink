using System;
using System.Collections.Generic;
using CrossLink;

namespace ConsoleApp1
{
    [CrossLinkObject]
    public partial class TestClass
    {
        [Link(Type = LinkType.Ordered)]
        private int id;

        [Link(Type = LinkType.Ordered)]
        public string name { get; private set; } = string.Empty;

        [Link(Type = LinkType.Ordered)]
        private int age;

        [Link(Type = LinkType.StackList, Name = "Stack")]
        [Link(Type = LinkType.List, Name = "List")]
        public TestClass(int id, string name, int age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override string ToString() => $"ID:{this.id, 2}, {this.name, -5}, {this.age, 2}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CrossLink Quick Start.");
            Console.WriteLine();

            var g = new TestClass.GoshujinClass(); // Goshujin (Owner) class
            new TestClass(1, "Hoge", 27).Goshujin = g; // Set Goshujin (Owner)
            new TestClass(2, "Fuga", 15).Goshujin = g;
            new TestClass(1, "A", 7).Goshujin = g;
            new TestClass(0, "Zero", 50).Goshujin = g;

            ConsoleWriteIEnumerable("[List]", g.ListChain); // ListChain is virtually a List<TestClass>

            Console.WriteLine("ListChain[2] : ");
            Console.WriteLine(g.ListChain[2]);
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Sorted by Id]", g.IdChain); 
            ConsoleWriteIEnumerable("[Sorted by Name]", g.NameChain);

            ConsoleWriteIEnumerable("[Sorted by Height]", g.AgeChain);
            var t = g.ListChain[1];
            Console.WriteLine($"{t.Name} age {t.Age} => 95");
            t.Age = 95;
            ConsoleWriteIEnumerable("[Sorted by Height]", g.AgeChain);

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);

            t = g.StackChain.Pop();
            Console.WriteLine($"{t.Name} => Pop");
            g.Remove(t);
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);

            var g2 = new TestClass.GoshujinClass(); // New Goshujin
            t = g.ListChain[0];
            Console.WriteLine($"{t.Name} Goshujin => Goshujin2");
            Console.WriteLine();
            t.Goshujin = g2; // Change Goshujin
            ConsoleWriteIEnumerable("[Goshujin]", g.ListChain);
            ConsoleWriteIEnumerable("[Goshujin2]", g2.ListChain);


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
