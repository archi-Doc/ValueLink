// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Interface for referencing a Goshujin instance from an object.
/// </summary>
public interface IObjectToGoshujin
{
    /// <summary>
    /// Gets a Goshujin instance.
    /// </summary>
    IGoshujin? Goshujin { get; }
}
