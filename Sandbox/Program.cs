using System;
using System.ComponentModel;
using CrossLink;

namespace Sandbox
{
    /*[CrossLinkObject()]
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

        [Link(Type = LinkType.StackList, Name = "name2")]
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
    }*/

    [CrossLinkObject]
    public partial class TestNotifyPropertyChanged : INotifyPropertyChanged
    {
        [Link(AutoNotify = true)]
        private int id;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [CrossLinkObject(ExplicitPropertyChanged = "propertyChanged")]
    public partial class TestNotifyPropertyChanged2 : INotifyPropertyChanged
    {
        [Link(AutoNotify = true)]
        private int id;

        public event PropertyChangedEventHandler? propertyChanged;

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this.propertyChanged += value;
            }

            remove
            {
                this.propertyChanged -= value;
            }
        }
    }

    [CrossLinkObject]
    public partial class TestNotifyPropertyChanged3
    {
        [Link(AutoNotify = true)]
        private int id;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // var tc3 = new TestClass3<int>(1, "test");
            // var g = new TestClass3<int>.GoshujinClass();
            // tc3.Goshujin = g;

            var count = 0;
            var t1 = new TestNotifyPropertyChanged();
            t1.PropertyChanged += (s, e) => { if (e.PropertyName == "Id") count++; };
            t1.Id = 1;
        }
    }
}
