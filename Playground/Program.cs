using System;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using ValueLink.Integrality;
using static Playground.SpClass;

namespace Playground;

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
public partial class SpClassPoint : ILockableData<SpClass>
{// Value, Link
    [Key(1)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    ValueTask<SpClass?> ILockableData<SpClass>.TryGet(TimeSpan timeout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ValueTask<DataScope<SpClass>> ILockableData<SpClass>.TryLock(TimeSpan timeout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

[TinyhandObject(Structual = true)]
public partial class SpClass
{
    public SpClass()
    {
    }

    [Key(0)]
    public string Name { get; private set; } = string.Empty;
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World");
    }
}
