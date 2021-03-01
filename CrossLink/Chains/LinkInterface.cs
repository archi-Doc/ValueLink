// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1615 // Element return value should be documented

namespace CrossLink
{
    public interface ILink<T>
    {
        T Object { get; }

        bool IsLinked { get; }
    }

    /*
    /// <summary>
    /// Defines methods to manipulate generic chains.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the chain.</typeparam>
    public interface IChain<T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets the number of links (elements) contained in the chain.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a link to the chain.
        /// </summary>
        /// <param name="link">The link to add to the chain.</param>
        void Add(ILink<T> link);

        /// <summary>
        /// Removes all items from the chain.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes the link from the chain.
        /// </summary>
        /// <param name="link">The link to remove from the chain.</param>
        /// <returns>True if the link was successfully removed from the chain.</returns>
        bool Remove(ILink<T> link);
    }
    */
}
