using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;
using ValueLink;

namespace Playground;

[TinyhandObject(Structual = true)]
public partial class LockedDataMock<TData> : ILockableData<TData>, IUnlockableData
    where TData : notnull
{
    private readonly SemaphoreLock lockObject = new();
    private TData? data;

    public LockedDataMock()
    {
    }

    public LockedDataMock(TData data)
    {
        this.data = data;
    }

    public void SetData(TData data)
    {
        using (this.lockObject.EnterScope())
        {
            this.data = data;
        }
    }

    public ValueTask<TData?> TryGet(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<TData?>(this.data);
    }

    public async ValueTask<DataScope<TData>> TryLock(TimeSpan timeout, CancellationToken cancellationToken)
    {
        await this.lockObject.EnterAsync(timeout, cancellationToken).ConfigureAwait(false);
        if (this.data is null)
        {
            return new(DataLockResult.NotFound);
        }
        else
        {
            return new(this.data, this);
        }
    }

    public void Unlock()
    {
        this.lockObject.Exit();
    }
}

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
public partial class SpClassPoint : LockedDataMock<SpClass>
{// Value, Link
    [Key(1)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    public SpClassPoint()
    {
        this.SetData(new());
    }
}

[TinyhandObject(Structual = true)]
public partial class SpClass
{
    public SpClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;
}

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World");

        var g = new SpClassPoint.GoshujinClass();
        using (var scope = await g.TryLock(123, LockMode.GetOrCreate))
        {
            if (scope.IsValid)
            {
                scope.Data.Name = "Test Name";
            }
        }

        using (var scope = await g.TryLock(123, LockMode.Get))
        {
            if (scope.IsValid)
            {
            }
        }

        var array = g.GetArray();
        foreach (var x in array)
        {
        }
    }

    private IEnumerable Get() => System.Array.Empty<object>();
}
