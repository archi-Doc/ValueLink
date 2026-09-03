// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Dispatches generated ownership operations through static interface members.
/// </summary>
/// <typeparam name="TGoshujin">The generated owner type.</typeparam>
/// <typeparam name="TObject">The owned object type.</typeparam>
public static class ValueLinkInternalHelper<TGoshujin, TObject>
    where TGoshujin : class, IGoshujin
    where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
{
    public static void AddToGoshujin(TObject obj, TGoshujin? goshujin, bool writeJournal = true)
        => TObject.AddToGoshujin(obj, goshujin, writeJournal);

    public static bool RemoveFromGoshujin(TObject obj, TGoshujin? goshujin, bool writeJournal = true)
        => TObject.RemoveFromGoshujin(obj, goshujin, writeJournal);

    public static void SetGoshujin(TObject obj, TGoshujin? goshujin)
        => TObject.SetGoshujin(obj, goshujin);
}

/// <summary>
/// Defines static ownership operations implemented by generated linked types.
/// </summary>
/// <typeparam name="TGoshujin">The type of the goshujin.</typeparam>
/// <typeparam name="TObject">The type of the object.</typeparam>
public interface IValueLinkObjectInternal<TGoshujin, TObject>
    where TGoshujin : class, IGoshujin
    where TObject : class
{
    static abstract void AddToGoshujin(TObject obj, TGoshujin? goshujin, bool writeJournal);

    static abstract bool RemoveFromGoshujin(TObject obj, TGoshujin? goshujin, bool writeJournal);

    static abstract void SetGoshujin(TObject obj, TGoshujin? goshujin);
}
