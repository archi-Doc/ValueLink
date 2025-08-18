// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

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
    public readonly DataLockResult Result;
    private readonly IDataUnlock dataUnlock;
    private TData? data;

    /// <summary>
    /// Gets the scoped data instance while the scope is valid; otherwise <c>null</c> after disposal or if lock failed.
    /// </summary>
    public TData? Data => this.data;

    /// <summary>
    /// Gets a value indicating whether the scope currently references valid data<br/>
    /// (i.e., it has not been disposed and the underlying data was successfully obtained).
    /// </summary>
    [MemberNotNullWhen(true, nameof(data))]
    public bool IsValid => this.data is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataScope{TData}"/> struct.<br/>
    /// Associates the scope with a lock result, an unlock handler, and the scoped data instance.
    /// </summary>
    /// <param name="result">The result of the data lock attempt.</param>
    /// <param name="dataUnlock">The unlock handler responsible for releasing the lock.</param>
    /// <param name="data">The data instance to be scoped; may be <c>null</c> if the lock failed.</param>
    public DataScope(DataLockResult result, IDataUnlock dataUnlock, TData? data)
    {
        this.Result = result;
        this.dataUnlock = dataUnlock;
        this.data = data;
    }

    /// <summary>
    /// Releases the lock associated with this scope (if still valid).<br/>
    /// Subsequent access to <see cref="Data"/> will return <c>null</c>.
    /// </summary>
    public void Dispose()
    {
        if (this.data is not null)
        {
            this.dataUnlock.Unlock();
            this.data = default;
        }
    }
}
