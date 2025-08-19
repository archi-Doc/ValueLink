// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Represents the state of a lockable data resource.
/// </summary>
public enum LockableDataState
{
    /// <summary>
    /// The data is not protected and can be deleted.
    /// </summary>
    Unprotected,

    /// <summary>
    /// The data is protected from deletion.
    /// </summary>
    Protected,

    /// <summary>
    /// The data has been deleted and is no longer available.
    /// </summary>
    Deleted,
}

/// <summary>
/// Defines an interface for acquiring and releasing a lock on a data resource.
/// </summary>
/// <typeparam name="TData">
/// The type of data to be locked. Must be non-null.
/// </typeparam>
public interface ILockableData<TData> // : IUnlockableData
    where TData : notnull
{
    /// <summary>
    /// Gets or sets the current state of the lockable data resource.
    /// </summary>
    LockableDataState State { get; set; }

    /// <summary>
    /// Attempts to retrieve the data instance if available, without acquiring a lock.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the lock. If <see cref="TimeSpan.Zero"/>, the method returns immediately.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting to acquire the lock.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> containing the data instance of type <typeparamref name="TData"/> if available; otherwise <c>null</c>.
    /// </returns>
    ValueTask<TData?> TryGet(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to acquire a lock on the data resource asynchronously.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the lock. If <see cref="TimeSpan.Zero"/>, the method returns immediately.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting to acquire the lock.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> containing a tuple with the <see cref="DataScopeResult"/><br/>
    /// indicating the outcome of the lock attempt, and the locked data if successful.
    /// </returns>
    ValueTask<DataScope<TData>> TryLock(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Releases the lock on the data resource.
    /// </summary>
    void Unlock();
}
