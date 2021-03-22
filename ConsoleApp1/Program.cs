using System;
using CrossLink;

namespace ConsoleApp1
{
    [CrossLinkObject()]
    public partial class TestClass
    {
        [Link(Name = "Test", Type = LinkType.LinkedList)]
        private int id;

        [Link(Type = LinkType.Ordered)]
        private string name;

        public TestClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    [CrossLinkObject(GoshujinClass = "Goshu", GoshujinInstance = "Instance")]
    public partial class TestClass2
    {
        [Link(Type = LinkType.Ordered)]
        private int id;

        [Link(Type = LinkType.Ordered, Name = "Name2")]
        private string Name { get; set; }

        public TestClass2(int id, string name)
        {
            this.id = id;
            this.Name = name;
        }
    }

    [CrossLinkObject]
    public partial class TestClass3<T>
    {
        [Link(Type = LinkType.Ordered)]
        private T id { get; set; }

        [Link(Type = LinkType.Ordered)]
        private string name { get; set; }

        public TestClass3(T id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    public partial class TestClass4
    {
        // [Link(Type = LinkType.Ordered)]
        private int id { get; set; }

        // [Link(Type = LinkType.Ordered)]
        private string name { get; set; }

        public TestClass4(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        [CrossLinkObject]
        partial class NestedClass
        {
            [Link(Type = LinkType.Ordered)]
            private uint id { get; set; }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var tc = new TestClass(1, "1");
            var tc0 = new TestClass(0, "0");
            var g = new TestClass.GoshujinClass();
            tc.Goshujin = g;
            tc0.Goshujin = g;

            var tc3 = new TestClass3<int>(3, "3");
            var g3 = new TestClass3<int>.GoshujinClass();
            tc3.Goshujin = g3;
        }
    }
}
