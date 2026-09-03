// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace ValueLink.Internal;

internal static class ChainHelper
{
    internal static void CopyTo<T>(IReadOnlyCollection<T> source, Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1 || array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("The destination must be a zero-based, one-dimensional array.", nameof(array));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index > array.Length || source.Count > array.Length - index)
        {
            throw new ArgumentException("The destination array has insufficient space.", nameof(array));
        }

        try
        {
            if (array is T[] typed)
            {
                foreach (var item in source)
                {
                    typed[index++] = item;
                }
            }
            else if (array is object?[] objects)
            {
                foreach (var item in source)
                {
                    objects[index++] = item;
                }
            }
            else
            {
                throw new ArgumentException("The destination array type is incompatible.", nameof(array));
            }
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("The destination array type is incompatible.", nameof(array));
        }
    }
}
