using System;
using CrossLink;

namespace Sandbox
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

            var tc3 = new TestClass3<int>(1, "test");
        }
    }
}
