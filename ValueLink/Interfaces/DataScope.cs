// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Tinyhand;

namespace ValueLink;

/// <summary>
/// Holds acquired data and releases its associated lock on disposal.
/// </summary>
/// <typeparam name="TData">Type of the data instance managed by this scope. Must be a non-nullable type.</typeparam>
/// <remarks>
/// Dispose only one copy of this mutable struct. For value-type data, inspect Result: the non-null data checks do not indicate lock success or disposal.
/// </remarks>
public record struct DataScope<TData> : IDisposable
    where TData : notnull
{
    /// <summary>
    /// Records the acquisition result, which remains available after disposal.
    /// </summary>
    public readonly DataScopeResult Result;
    // public readonly bool NewlyCreated; // We considered adding NewlyCreated, but since TryLock does not always succeed, the determination and initialization of NewlyCreated will be handled on the object side rather than in DataScope.
    private TData? data;
    private IDataUnlocker? dataUnlocker;
    private IStructuralObject? structuralObject;

    /// <summary>
    /// Gets the scoped data, or default after disposal or an unsuccessful acquisition.
    /// </summary>
    public TData? Data => this.data;

    /// <summary>
    /// Gets a value indicating whether the scoped data is non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsValid => this.data is not null;

    /// <summary>
    /// Gets a value indicating whether the result is Retrieved and the scoped data is non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsRetrieved => this.Result == DataScopeResult.Retrieved && this.data is not null;

    /// <summary>
    /// Gets a value indicating whether the result is Created and the scoped data is non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsCreated => this.Result == DataScopeResult.Created && this.data is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataScope{TData}"/> struct with acquired data and its lock-release adapter.
    /// </summary>
    /// <param name="result">The result indicating whether the data was retrieved, created, or another status.</param>
    /// <param name="data">The data instance to be scoped and locked.</param>
    /// <param name="dataUnlocker">The data instance responsible for releasing the lock on the data resource.</param>
    /// <param name="structuralObject">The structural object associated with the data, used for deletion if needed.</param>
    public DataScope(DataScopeResult result, TData data, IDataUnlocker dataUnlocker, IStructuralObject structuralObject)
    {
        this.Result = result;
        this.data = data;
        this.dataUnlocker = dataUnlocker;
        this.structuralObject = structuralObject;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataScope{TData}"/> struct with an acquisition result and no owned lock.
    /// </summary>
    /// <param name="result">
    /// The result of the data lock attempt, indicating the reason for failure or status.
    /// </param>
    public DataScope(DataScopeResult result)
    {
        this.Result = result;
    }

    /// <summary>
    /// Gets the current control state from the data associated with this scope.
    /// </summary>
    /// <returns>
    /// The current <see cref="DataControlState"/> when the scope is still linked to an unlocker;
    /// otherwise <see langword="default"/> if the scope is no longer valid.
    /// </returns>
    public DataControlState GetControlState()
    {
        if (this.dataUnlocker is { } dataUnlocker)
        {
            return dataUnlocker.GetControlState();
        }

        return default;
    }

    /// <summary>
    /// Sets the control state on the data associated with this scope.
    /// </summary>
    /// <param name="state">The <see cref="DataControlState"/> value to apply.</param>
    /// <remarks>
    /// If the scope has already been disposed or does not contain an unlocker, this call has no effect.
    /// </remarks>
    public void SetControlState(DataControlState state)
    {
        this.dataUnlocker?.SetControlState(state);
    }

    /// <summary>
    /// Releases the lock and requests structural deletion if the unlocker accepts deletion.
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
    /// Releases the associated lock and resets the data to its default value.
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
