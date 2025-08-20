// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// An interface that provides a counter for protecting data resources.
/// </summary>
public interface IDataProtectionCounter
{
    /// <summary>
    /// Gets a reference to the protection counter.<br/>
    /// &lt; 0: The data is deleted<br/>
    /// = 0: The data is not protected (can be deleted).<br/>
    /// &gt; 0: The data is protected.
    /// </summary>
    /// <returns>
    /// A reference to an <see cref="int"/> representing the protection counter.
    /// </returns>
    ref int GetProtectionCounterRef();
}
