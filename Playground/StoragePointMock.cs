using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;

namespace CrystalData;

public class StoragePoint<TData> : IStructualObject, IDataLocker<TData>
    where TData : notnull
{
    private ObjectProtectionState protectionState;

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }

    public void SetData(TData data)
    {
    }

    ref ObjectProtectionState IDataLocker<TData>.GetProtectionStateRef() => ref this.protectionState;

    public ValueTask<TData?> TryGet() => ValueTask.FromResult<TData?>(default);

    public ValueTask<DataScope<TData>> TryLock(AcquisitionMode acquisitionMode, TimeSpan timeout, CancellationToken cancellationToken = default) => ValueTask.FromResult(new DataScope<TData>(DataScopeResult.Timeout));

    ValueTask<TData?> IDataLocker<TData>.TryGet(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<TData?>(default);
    }

    ValueTask<DataScope<TData>> IDataLocker<TData>.TryLock(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new DataScope<TData>(DataScopeResult.Timeout));
    }

    public Task DeleteData(DateTime forceDeleteAfter = default)
    {
        return Task.CompletedTask;
    }

    Task IDataLocker<TData>.DeletePoint(DateTime forceDeleteAfter)
    {
        throw new NotImplementedException();
    }
}
