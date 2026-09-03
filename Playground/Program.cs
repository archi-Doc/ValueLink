using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CrystalData;
using Tinyhand;
using ValueLink;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace QuickStart.Evolution
{

    /// <summary>
    /// Provides serializable data for storage-point experiments.
    /// </summary>
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

        [IgnoreMember]
        public partial GoshujinClass? Goshujin { get; set; }

        public override string ToString()
            => this.Id.ToString();
    }

    /// <summary>
    /// Demonstrates a generated owner for read-committed storage points.
    /// </summary>
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
    /// <summary>
    /// Provides linked data for playground experiments.
    /// </summary>
    [ValueLinkObject]
    public partial class TestClass
    {
        /// <summary>
        /// Extends the playground owner with experimental operations.
        /// </summary>
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
