// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

public static class ValueLinkInternalHelper<TGoshujin, TObject>
    where TGoshujin : class, IGoshujin
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
{
    public static void AddToGoshujin(TObject obj, TGoshujin? g, bool writeJournal = true)
        => TObject.AddToGoshujin(obj, g, writeJournal);

    public static bool RemoveFromGoshujin(TObject obj, TGoshujin? g, bool erase, bool writeJournal = true)
        => TObject.RemoveFromGoshujin(obj, g, erase, writeJournal);
}

/// <summary>
/// An internal interface for value link object.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the object.</typeparam>
public interface IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : class, IGoshujin
    where TObject : class
{
    static abstract void AddToGoshujin(TObject obj, TGoshujin? g, bool writeJournal);

    static abstract bool RemoveFromGoshujin(TObject obj, TGoshujin? g, bool erase, bool writeJournal);
}
