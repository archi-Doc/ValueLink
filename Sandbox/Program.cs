using System;
using CrossLink;

namespace Sandbox
{
    public partial class TestClass
    {
        [CrossLink(Name = "test", Type = LinkType.LinkedList)]
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
