using System;
using CrossLink;

namespace Sandbox
{
    [CrossLinkObject()]
    public partial class TestClass
    {
        [Link(Name = "Test", Type = LinkType.LinkedList)]
        private int id { get; set; }

        [Link(Type = LinkType.Ordered)]
        private string name { get; set; }

        public TestClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
