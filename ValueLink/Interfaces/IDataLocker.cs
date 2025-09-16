// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Defines an interface for acquiring and releasing a lock on a data resource.
/// </summary>
/// <typeparam name="TData">
/// The type of data to be locked. Must be non-null.
/// </typeparam>
public interface IDataLocker<TData>
    where TData : notnull
{
    ref ObjectProtectionState GetProtectionStateRef();

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
    /// Deletes the data point, optionally forcing deletion after the specified date and time.
    /// If the object is protected, waits until it can be deleted or until <paramref name="forceDeleteAfter"/> is reached.
    /// </summary>
    /// <param name="forceDeleteAfter">
    /// The time after which the deletion will be forced even if the object is protected.<br/>
    /// If <see cref="DateTime.MinValue"/>, waits indefinitely.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
    Task DeletePoint(DateTime forceDeleteAfter);
}
