using System;
using CrossLink;

namespace Sandbox
{
    [CrossLinkObject()]
    public partial class TestClass
    {
        [Link(Name = "test", Type = LinkType.LinkedList)]
        public int ID { get; set; }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
