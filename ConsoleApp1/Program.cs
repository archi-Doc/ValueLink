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
        public int age { get; private set; }

        [Link(Type = LinkType.Ordered)]
        private double height;

        [Link(Type = LinkType.StackList, Name = "Stack")]
        [Link(Type = LinkType.List, Name = "List")]
        public TestClass(int id, string name, int age, double height)
        {
            this.id = id;
            this.name = name;
            this.age = age;
            this.height = height;
        }

        public override string ToString() => $"ID:{this.id, 2}, {this.name, -5}, Age:{this.age, 3}, Height:{this.height:F2}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CrossLink Quick Start.");
            Console.WriteLine();

            var g = new TestClass.GoshujinClass(); // Goshujin (Owner) class
            new TestClass(1, "Hoge", 17, 1.7).Goshujin = g; // Set Goshujin (Owner)
            new TestClass(2, "Fuga", 18, 1.5).Goshujin = g;
            new TestClass(3, "A", 77, 1.2).Goshujin = g;
            new TestClass(0, "Zero", 100, 0).Goshujin = g;

            ConsoleWriteIEnumerable("[List]", g.ListChain);

            Console.WriteLine("List[2]");
            Console.WriteLine(g.ListChain[2]);
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Sorted by Id]", g.IdChain);
            ConsoleWriteIEnumerable("[Sorted by Name]", g.NameChain);
            ConsoleWriteIEnumerable("[Sorted by Age]", g.AgeChain);
            ConsoleWriteIEnumerable("[Sorted by Height]", g.HeightChain);
            ConsoleWriteIEnumerable("[Stack]", g.StackChain);

            Console.WriteLine("Pop");
            var pc = g.StackChain.Pop();
            g.Remove(pc);
            Console.WriteLine(pc);
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            ConsoleWriteIEnumerable("[Sorted by Id]", g.IdChain);

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
