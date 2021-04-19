using System;
using System.ComponentModel;
using CrossLink;
using Tinyhand;

namespace Sandbox
{
    [CrossLinkObject]
    [TinyhandObject]
    public partial class TestClass
    {
        [Link(Name = "Test", Primary = true, Type = LinkType.LinkedList)]
        [KeyAsName]
        private int id;

        [Link(Type = LinkType.ReverseOrdered)]
        [KeyAsName]
        private string name;

        [Link(AutoNotify = true)]
        [KeyAsName]
        private uint uid;

        [Link(Name = "Test11", Type = LinkType.LinkedList)]
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
    }*/

    [CrossLinkObject]
    [TinyhandObject]
    public partial class TestClass3<T>
    {
        [Link(Type = LinkType.Ordered)]
        [KeyAsName]
        private T id { get; set; }

        [Link(Type = LinkType.StackList, Name = "name2", AutoLink = false)]
        [KeyAsName]
        private string name { get; set; }

        [Link(Type = LinkType.StackList, Primary = true, Name = "Stack")]
        public TestClass3(T id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public TestClass3()
        {
            this.id = default!;
            this.name = string.Empty;
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
        [TinyhandObject]
        partial class NestedClass
        {
            [Link(Type = LinkType.Ordered, Primary = true)]
            [KeyAsName]
            private uint id { get; set; }
        }
    }

    [CrossLinkObject]
    public partial class ReverseTestClass
    {
        [Link(Primary = true, Type = LinkType.ReverseOrdered)]
        [KeyAsName]
        private int id;

        [Link(Type = LinkType.Unordered)]
        [KeyAsName]
        private string name;

        public ReverseTestClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public ReverseTestClass()
        {
            this.id = 0;
            this.name = string.Empty;
        }

        public override string ToString() => $"{this.id} : {this.name}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var gr = new ReverseTestClass.GoshujinClass();
            new ReverseTestClass(1, "1").Goshujin = gr;
            new ReverseTestClass(2, "2").Goshujin = gr;
            new ReverseTestClass(3, "3").Goshujin = gr;
            new ReverseTestClass(0, "0").Goshujin = gr;

            foreach (var x in gr.IdChain)
            {
                Console.WriteLine(x);
            }

            Console.WriteLine();

            foreach (var x in gr.NameChain.Enumerate("2"))
            {
                Console.WriteLine(x);
            }

            var rc = gr.NameChain.FindFirst("6");
            rc = gr.NameChain.FindFirst("3");

            Console.WriteLine();

            var tc3 = new TestClass3<int>(1, "test");
            var g3 = new TestClass3<int>.GoshujinClass();
            tc3.Goshujin = g3;
            new TestClass3<int>(2, "test2").Goshujin = g3;

            /*var g = new SentinelClass.SentinelGoshujin();
            new SentinelClass(1, "a").Goshujin = g;
            new SentinelClass(2, "b").Goshujin = g;
            new SentinelClass(3, "0").Goshujin = g;*/

            var g = new TestClass.GoshujinClass();
            new TestClass(1, "a").Goshujin = g;
            new TestClass(2, "b").Goshujin = g;
            new TestClass(3, "0").Goshujin = g;
            new TestClass(29, "b").Goshujin = g;

            foreach (var x in g.NameChain)
            {
                Console.WriteLine(x.Test);
            }

            var b = TinyhandSerializer.Serialize(g);
            var st = TinyhandSerializer.SerializeToString(g);
            var g2 = TinyhandSerializer.Deserialize<TestClass.GoshujinClass>(b);

            b = TinyhandSerializer.Serialize(g3);
            st = TinyhandSerializer.SerializeToString(g3);
            g3 = TinyhandSerializer.Deserialize<TestClass3<int>.GoshujinClass>(b);

            var g4 = new TestClass3<double>.GoshujinClass();
            new TestClass3<double>(2, "test2").Goshujin = g4;
            new TestClass3<double>(1.2, "test12").Goshujin = g4;
            new TestClass3<double>(2.1, "test21").Goshujin = g4;
            b = TinyhandSerializer.Serialize(g4);
            st = TinyhandSerializer.SerializeToString(g4);

            var tc = new TestClass3<double>(2, "test2");
            new TestClass3<double>(2, "test2").Goshujin = g4; ;
            tc.Goshujin = null;
        }
    }
}
