// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLink;
using Tinyhand;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1401 // Fields should be private

namespace ConsoleApp1
{
    [CrossLinkObject]
    public partial class TinyClass
    {// Tiny class to demonstrate how CrossLink works.
        [Link(Type = ChainType.Ordered)]
        private int id;

        public static void Test()
        {
            var g = new TinyClass.GoshujinClass();
            new TinyClass().Goshujin = g;
        }
    }

    [CrossLinkObject]
    [TinyhandObject] // Add a TinyhandObject attribute to use TinyhandSerializer.
    public partial class SerializeClass
    {
        [Link(Type = ChainType.Ordered, Primary = true)] // Set primary link that is guaranteed to holds all objects in the collection in order to maximize the performance of serialization.
        [Key(0)] // Add a Key attribute to specify the key for serialization as a number or string.
        private int id;

        [Link(Type = ChainType.Ordered)]
        [Key(1)]
        private string name = default!;

        public SerializeClass()
        {// Default constructor is required for Tinyhand.
        }

        public SerializeClass(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public static void Test()
        {
            var g = new SerializeClass.GoshujinClass(); // Create a new Goshujin.
            new SerializeClass(1, "Hoge").Goshujin = g; // Add an object.
            new SerializeClass(2, "Fuga").Goshujin = g;

            var st = TinyhandSerializer.SerializeToString(g); // Serialize to string.
            var g2 = TinyhandSerializer.Deserialize<SerializeClass.GoshujinClass>(TinyhandSerializer.Serialize(g)); // Serialize to a byte array and deserialize it.
        }
    }

    [CrossLinkObject]
    public partial class AutoNotifyClass
    {
        [Link(AutoNotify = true)] // Set AutoNotify to true.
        private int id;

        public void Reset()
        {
            this.SetProperty(ref this.id, 0); // Change the value manually and invoke PropertyChanged.
        }

        public static void Test()
        {
            var c = new AutoNotifyClass();
            c.PropertyChanged += (s, e) => { Console.WriteLine($"Id changed: {((AutoNotifyClass)s!).Id}"); };

            c.Id = 1; // Change the value and automatically invoke PropertyChange.
            c.Reset(); // Reset the value.
        }
    }

    [CrossLinkObject]
    public partial class ManualLinkClass
    {
        [Link(Type = ChainType.Ordered, AutoLink = false)] // Set AutoLink to false.
        private int id;

        public ManualLinkClass(int id)
        {
            this.id = id;
        }

        public static void Test()
        {
            var g = new ManualLinkClass.GoshujinClass();

            var c = new ManualLinkClass(1);
            c.Goshujin = g;
            Debug.Assert(g.IdChain.Count == 0, "Chain is empty.");

            g.IdChain.Add(c.id, c); // Link the object manually.
            Debug.Assert(g.IdChain.Count == 1, "Object is linked.");
        }
    }

    public class BaseClass
    {
        protected int id;

        protected string name = string.Empty;
    }

    [CrossLinkObject]
    public partial class DerivedClass : BaseClass
    {
        [Link(Type = ChainType.ReverseOrdered)]
        protected int id2;

        [Link(Type = ChainType.QueueList, Name = "Queue")]
        public DerivedClass()
        {
        }
    }

    /*[CrossLinkObject] // Error! Derivation from other CrossLink objects is not supported.
    public partial class DerivedClass2 : DerivedClass
    {
        [Link(Type = ChainType.Ordered)]
        protected string name2 = string.Empty;
    }*/
}
