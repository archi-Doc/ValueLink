using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;
using ValueLink;

namespace Playground;

[TinyhandObject(Structual = true)]
public partial class LockedDataMock<TData> : IDataLocker<TData>, IDataUnlocker, IStructualObject
    where TData : notnull
{
    private readonly SemaphoreLock lockObject = new();
    private TData? data;
    private ObjectProtectionState protectionState;

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }

    // ref int IDataLocker<TData>.GetProtectionCounterRef() => ref this.protectionCounter;

    ref ObjectProtectionState IDataLocker<TData>.GetProtectionStateRef() => ref this.protectionState;

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
        if (!await this.lockObject.EnterAsync(timeout, cancellationToken).ConfigureAwait(false))
        {
            return new(DataScopeResult.Timeout);
        }

        if (this.data is null)
        {
            return new(DataScopeResult.NotFound);
        }
        else
        {
            // Unprotected -> Protected
            if (Interlocked.CompareExchange(ref this.protectionState, ObjectProtectionState.Protected, ObjectProtectionState.Unprotected) != ObjectProtectionState.Unprotected)
            {// Protected(?) or Deleted
                return new(DataScopeResult.Obsolete);
            }

            return new(this.data, this);
        }
    }

    Task IDataLocker<TData>.DeletePoint(DateTime forceDeleteAfter, bool writeJournal)
    {
        throw new NotImplementedException();
    }

    public void Unlock()
    {
        // Protected -> Unprotected
        Interlocked.CompareExchange(ref this.protectionState, ObjectProtectionState.Unprotected, ObjectProtectionState.Protected);

        this.lockObject.Exit();
    }

    public Task<bool> StoreData(StoreMode storeMode)
    {
        if (this.data is IStructualObject structualObject)
        {
            return structualObject.StoreData(storeMode);
        }
        else
        {
            return Task.FromResult(true);
        }
    }

    public Task DeleteData(DateTime forceDeleteAfter, bool writeJournal)
    {
        if (this.data is IStructualObject structualObject)
        {
            return structualObject.DeleteData(forceDeleteAfter, writeJournal);
        }
        else
        {
            return Task.FromResult(true);
        }
    }

    Task IDataUnlocker.UnlockAndDelete(DateTime forceDeleteAfter)
    {
        throw new NotImplementedException();
    }

    void IDataUnlocker.Unlock()
    {
        throw new NotImplementedException();
    }
}

[TinyhandObject(Structual = true)]
public partial class SpClass
{
    [TinyhandObject(Structual = true)]
    [ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
    public partial class SpClassPoint : CrystalData.StoragePoint<SpClass> // LockedDataMock<SpClass>
    {// Value, Link
        [Key(1)]
        [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
        public int Id { get; set; }

        public SpClassPoint(int x)
        {
            this.SetData(new());
        }
    }

    public SpClass()
    {
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [Key(1)]
    public SpClassPoint.GoshujinClass Goshujin { get; set; } = new();
}

internal class Program
{
    static async Task Main(string[] args)
    {
        var n = Unsafe.SizeOf<DataScope<byte[]>>();
        Console.WriteLine($"Hello, World {n}");

        var a = new SpClass.SpClassPoint(111);
        var g = new SpClass.SpClassPoint.GoshujinClass();
        var tc = await g.TryGet(123);
        using (var scope = await g.TryLock(123, AcquisitionMode.GetOrCreate))
        {
            if (scope.IsValid)
            {
                scope.Data.Name = "Test Name";
            }
        }

        tc = await g.TryGet(123);
        using (var scope = await g.TryLock(123, AcquisitionMode.Get))
        {
            if (scope.IsValid)
            {

            }
        }

        var array = g.GetArray();
        foreach (var x in array)
        {
        }

        tc = await g.TryGet(123);
        await g.Delete(123);
        array = g.GetArray();

        var spc = new SpClass();
        using (var spd = await spc.Goshujin.TryLock(1, AcquisitionMode.GetOrCreate))
        {
            if (spd.IsValid)
            {
                await spd.Data.Goshujin.TryLock(2, AcquisitionMode.GetOrCreate);
            }
        }

        await spc.Goshujin.Delete(2);
        await spc.Goshujin.Delete(1, DateTime.UtcNow + TimeSpan.FromSeconds(2));
    }
}

internal static class Helper
{
}
