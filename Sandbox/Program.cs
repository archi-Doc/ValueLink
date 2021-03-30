using System;
using System.ComponentModel;
using CrossLink;
using Tinyhand;

namespace Sandbox
{
    [CrossLinkObject]
    // [TinyhandObject]
    public partial class TestClass
    {
        [Link(Name = "Test", Type = LinkType.LinkedList)]
        [KeyAsName]
        private int id;

        [Link(Type = LinkType.Ordered)]
        private string name;

        public TestClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public TestClass()
        {
            this.id = 0;
            this.name = string.Empty;
        }
    }

    /*[CrossLinkObject(GoshujinClass = "Goshu", GoshujinInstance = "Instance")]
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

        [Link(Type = LinkType.StackList, Name = "name2", AutoLink = false)]
        private string name { get; set; }

        [Link(Type = LinkType.StackList, Name = "Stack")]
        [Link(Type = LinkType.StackList, Name = "Stack2")]
        public TestClass3(T id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    public partial class TestClass4
    {
        [Link(Type = LinkType.Ordered)]
        private int id { get; set; }

        [Link(Type = LinkType.Ordered)]
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
    }*/

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            /*var tc3 = new TestClass3<int>(1, "test");
            var g = new TestClass3<int>.GoshujinClass();
            tc3.Goshujin = g;*/

            var g = new SentinelClass.SentinelGoshujin();
            new SentinelClass(1, "a").Goshujin = g;
            new SentinelClass(2, "b").Goshujin = g;
            new SentinelClass(3, "0").Goshujin = g;

            var b = TinyhandSerializer.Serialize(g);
            var st = TinyhandSerializer.SerializeToString(g);
            var g2 = TinyhandSerializer.Deserialize<SentinelClass.SentinelGoshujin>(b);
        }
    }
}
