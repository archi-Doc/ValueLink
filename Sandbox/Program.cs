using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ValueLink;
using Tinyhand;

namespace Sandbox
{
    [ValueLinkObject]
    [TinyhandObject]
    public partial class TestClass
    {
        [Link(Name = "Test", Primary = true, Type = ChainType.LinkedList)]
        [KeyAsName]
        private int id;

        [Link(Type = ChainType.Unordered)]
        [KeyAsName]
        private string name;

        [Link(AutoNotify = true)]
        [KeyAsName]
        private uint uid;

        [Link(Name = "Test11", Type = ChainType.LinkedList)]
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

    public class BaseClass
    {
        protected int id;

        protected string name = string.Empty;
    }

    [ValueLinkObject]
    public partial class DerivedClass : BaseClass
    {
        [Link(Type = ChainType.ReverseOrdered)]
        protected int id2;

        private int id3;

        [Link(Type = ChainType.QueueList, Name = "Queue")]
        [Link(Type = ChainType.QueueList, TargetMember = "id")]
        [Link(Type = ChainType.ReverseOrdered, TargetMember = "id3")]
        public DerivedClass()
        {
        }
    }

    /*[ValueLinkObject(GoshujinClass = "Goshu", GoshujinInstance = "Instance")]
    public partial class TestClass2
    {
        [Link(Type = ChainType.Ordered)]
        private int id;

        [Link(Type = ChainType.Ordered, Name = "Name2")]
        private string Name { get; set; }

        public TestClass2(int id, string name)
        {
            this.id = id;
            this.Name = name;
        }
    }*/

    [ValueLinkObject]
    [TinyhandObject]
    public partial class TestClass3<T>
    {
        [Link(Type = ChainType.Ordered)]
        [KeyAsName]
        private T id { get; set; }

        [Link(Type = ChainType.StackList, Name = "name2", AutoLink = false)]
        [KeyAsName]
        private string name { get; set; }

        [Link(Type = ChainType.StackList, Primary = true, Name = "Stack")]
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
        [Link(Type = ChainType.Ordered)]
        private int id { get; set; }

        [Link(Type = ChainType.Ordered)]
        private string name { get; set; }

        public TestClass4(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        [ValueLinkObject]
        [TinyhandObject]
        partial class NestedClass
        {
            [Link(Type = ChainType.Ordered, Primary = true)]
            [KeyAsName]
            private uint id { get; set; }
        }
    }

    [ValueLinkObject]
    public partial class ObservableClass
    {
        [Link(Type = ChainType.Ordered, AutoNotify = true)]
        private int id { get; set; }

        [Link(Type = ChainType.Observable, Name = "Observable")]
        public ObservableClass()
        {
        }
    }

    [ValueLinkObject]
    public partial class ReverseTestClass
    {
        [Link(Primary = true, Type = ChainType.ReverseOrdered)]
        [KeyAsName]
        private int id;

        [Link(Type = ChainType.Unordered)]
        [KeyAsName]
        private string name;

        [Link(Type = ChainType.Observable, Name = "Observable")]
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

            var f = g.NameChain.FindFirst("0");

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
