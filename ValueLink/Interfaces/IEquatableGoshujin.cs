// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// An interface for comparing goshujins.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
public interface IEquatableGoshujin<TGoshujin>
{
    bool GoshujinEquals(TGoshujin other);
}
