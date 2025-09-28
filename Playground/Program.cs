using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;
using ValueLink;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        var n = Unsafe.SizeOf<DataScope<byte[]>>();
        Console.WriteLine($"Hello, World {n}");
    }
}
