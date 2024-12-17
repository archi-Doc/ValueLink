// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arc.Collections;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace ValueLink;

/// <summary>
/// Helper functions for ValueLink.
/// </summary>
public static class ValueLinkHelper
{
    public static void AddToGoshujin<TGoshujin, TObject>(this ref TemporaryList<TObject> queue, TGoshujin goshujin)
        where TGoshujin : class, IGoshujin
        where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
    {
        foreach (var x in queue)
        {// Slow...
            TObject.SetGoshujin(x, goshujin);
        }
    }

    public static void RemoveFromGoshujin<TGoshujin, TObject>(this ref TemporaryList<TObject> queue)
        where TGoshujin : class, IGoshujin
        where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
    {
        foreach (var x in queue)
        {// Slow...
            TObject.SetGoshujin(x, default);
        }
    }

    public static void SetGoshujin2<TGoshujin, TObject>(ReadOnlySpan<TObject> objects, TGoshujin? goshujin)
        where TGoshujin : class, IGoshujin
        where TObject : class, IValueLinkObjectInternal<TGoshujin, TObject>
    {
        foreach (var x in objects)
        {
            TObject.SetGoshujin(x, goshujin);
        }
    }
}
