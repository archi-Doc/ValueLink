// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Tinyhand;

namespace ValueLink;

/// <summary>
/// Represents a scope over a locked data instance of type <typeparamref name="TData"/>,<br/>
/// providing controlled access and automatic lock management for the underlying storage object.<br/>
/// Disposing the scope releases the associated lock and invalidates the data reference.
/// </summary>
/// <typeparam name="TData">Type of the data instance managed by this scope. Must be a non-nullable type.</typeparam>
public record struct DataScope<TData> : IDisposable
    where TData : notnull
{// 32 bytes
    public readonly DataScopeResult Result;
    // public readonly bool NewlyCreated; // We considered adding NewlyCreated, but since TryLock does not always succeed, the determination and initialization of NewlyCreated will be handled on the object side rather than in DataScope.
    private TData? data;
    private IDataUnlocker? dataUnlocker;
    private IStructuralObject? structuralObject;

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
    /// <param name="structuralObject">The structural object associated with the data, used for deletion if needed.</param>
    public DataScope(TData data, IDataUnlocker dataUnlocker, IStructuralObject structuralObject)
    {
        this.Result = DataScopeResult.Success;
        this.data = data;
        this.dataUnlocker = dataUnlocker;
        this.structuralObject = structuralObject;
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
    /// Releases the lock on the data resource and deletes it, optionally forcing deletion after the specified date and time.
    /// </summary>
    /// <param name="forceDeleteAfter">
    /// The time after which the deletion will be forced even if the object is protected.<br/>
    /// If <see langword="default"/>, waits indefinitely.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
    public Task UnlockAndDelete(DateTime forceDeleteAfter = default)
    {
        this.data = default;
        if (this.dataUnlocker is { } dataUnlocker)
        {
            this.dataUnlocker = default;
            if (dataUnlocker.UnlockAndDelete())
            {// Deleted
                if (this.structuralObject is { } structuralObject)
                {
                    this.structuralObject = default;
                    return structuralObject.DeleteData(forceDeleteAfter, true);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the lock associated with this scope (if still valid).<br/>
    /// Subsequent access to <see cref="Data"/> will return <c>null</c>.
    /// </summary>
    public void Dispose()
    {
        if (this.dataUnlocker is not null)
        {
            this.dataUnlocker.Unlock();
        }

        this.data = default;
        this.dataUnlocker = default;
        this.structuralObject = default;
    }
}
