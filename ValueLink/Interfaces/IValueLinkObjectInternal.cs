// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// An internal interface for value link object.
/// </summary>
/// <typeparam name="TGoshujin">The type of goshujin class.</typeparam>
public interface IValueLinkObjectInternal<TGoshujin>
    where TGoshujin : class
{
    // void AddToGoshujinInternal(TGoshujin g);

    bool RemoveFromGoshujinInternal(TGoshujin? g);
}
