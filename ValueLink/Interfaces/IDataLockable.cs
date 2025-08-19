// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace ValueLink;

/// <summary>
/// Defines an interface for acquiring and releasing a lock on a data resource.
/// </summary>
/// <typeparam name="TData">
/// The type of data to be locked. Must be non-null.
/// </typeparam>
public interface IDataLockable<TData> : IDataUnlockable
    where TData : notnull
{
    /// <summary>
    /// Attempts to acquire a lock on the data resource asynchronously.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> containing a tuple with the <see cref="DataLockResult"/><br/>
    /// indicating the outcome of the lock attempt, and the locked data if successful.
    /// </returns>
    ValueTask<DataScope<TData>> TryLock();
}
