// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink
{
    /// <summary>
    /// Link interface.
    /// </summary>
    /// <typeparam name="T">The type of the object to be linked.</typeparam>
    public interface ILink<T>
    {
        /// <summary>
        /// Gets a value indicating whether an object is linked.
        /// </summary>
        bool IsLinked { get; }
    }
}
