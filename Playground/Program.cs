using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using CrystalData;
using Tinyhand;
using ValueLink;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace QuickStart.Evolution
{

    [TinyhandObject(LockObject = "syncObject")]
    [ValueLinkObject]
    public partial class Class1
    {
        [Key(0)]
        [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
        public int Id { get; set; }

        private readonly Lock syncObject = new();

        public Class1(int id)
        {
            this.Id = id;
        }

        public void Test()
        {
            using (this.syncObject.EnterScope())
            {
                this.Id++;
                Console.WriteLine($"Class1: {this}");
            }
        }

        public override string ToString()
            => this.Id.ToString();
    }

    [TinyhandObject(Structural = true)]
    [ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
    public partial class Class1Point : StoragePoint<Class1>
    {
        [Key(1)]
        [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
        public int Id { get; private set; }

        public Class1Point(int id)
        {
        }
    }
}

namespace Playground
{
    [ValueLinkObject]
    public partial class TestClass
    {
        public partial class GoshujinClass
        {
            public void Test()
            {
                var array = this.ToArray();
            }
        }

        [Link(Primary = true, Type = ChainType.Unordered)]
        public int Id { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            var n = Unsafe.SizeOf<DataScope<byte[]>>();
            Console.WriteLine($"Hello, World {n}");
        }
    }
}
