using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;
using ValueLink;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Playground;

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
