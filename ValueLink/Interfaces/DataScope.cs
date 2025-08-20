// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ValueLink;

/// <summary>
/// Represents a scope over a locked data instance of type <typeparamref name="TData"/>,<br/>
/// providing controlled access and automatic lock management for the underlying storage object.<br/>
/// Disposing the scope releases the associated lock and invalidates the data reference.
/// </summary>
/// <typeparam name="TData">Type of the data instance managed by this scope. Must be a non-nullable type.</typeparam>
public record struct DataScope<TData> : IDisposable
    where TData : notnull
{
    public readonly DataScopeResult Result;
    private TData? data;
    private IDataUnlocker? dataUnlocker;

    /// <summary>
    /// Gets the scoped data instance while the scope is valid; otherwise <c>null</c> after disposal or if lock failed.
    /// </summary>
    public TData? Data => this.data;

    /// <summary>
    /// Gets a value indicating whether the scope currently references valid data<br/>
    /// (i.e., it has not been disposed and the underlying data was successfully obtained).
    /// </summary>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsValid => this.data is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataScope{TData}"/> struct with a valid data instance and unlocker.<br/>
    /// The scope is considered valid and will automatically release the lock upon disposal.
    /// </summary>
    /// <param name="data">The data instance to be scoped and locked.</param>
    /// <param name="dataUnlocker">The data instance responsible for releasing the lock on the data resource.</param>
    public DataScope(TData data, IDataUnlocker dataUnlocker)
    {
        this.Result = DataScopeResult.Success;
        this.data = data;
        this.dataUnlocker = dataUnlocker;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataScope{TData}"/> struct with a specified lock result.<br/>
    /// This constructor is used when the lock attempt did not yield a valid data instance.
    /// </summary>
    /// <param name="result">
    /// The result of the data lock attempt, indicating the reason for failure or status.
    /// </param>
    public DataScope(DataScopeResult result)
    {
        this.Result = result;
    }

    /// <summary>
    /// Releases the lock associated with this scope (if still valid).<br/>
    /// Subsequent access to <see cref="Data"/> will return <c>null</c>.
    /// </summary>
    public void Dispose()
    {
        this.data = default;
        if (this.dataUnlocker is not null)
        {
            this.dataUnlocker.Unlock();
            this.dataUnlocker = default;
        }
    }
}
