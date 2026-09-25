// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ValueLink.Internal;

internal static class ChainHelper
{
    /// <summary>
    /// Copies the objects of an owner's primary chain; an owner without one yields an empty array.
    /// </summary>
    /// <typeparam name="T">The type of owned objects.</typeparam>
    /// <param name="goshujin">The owner, whose lock the caller must hold when required.</param>
    /// <returns>A snapshot of object references in primary-chain order.</returns>
    internal static T[] ToArray<T>(IGoshujin goshujin)
    {
        var objects = goshujin.EnumerateObjects();
        T[] array;
        if (objects is ICollection<T> collection)
        {// ListChain: a single array copy.
            if (collection.Count == 0)
            {
                return [];
            }

            array = new T[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }
        else if (objects is ICollection nonGeneric)
        {// Other chains copy through their struct enumerators; an owner without a primary chain returns an empty object array.
            if (nonGeneric.Count == 0)
            {
                return [];
            }

            array = new T[nonGeneric.Count];
            nonGeneric.CopyTo(array, 0);
            return array;
        }
        else if (objects is IEnumerable<T> enumerable)
        {
            return enumerable.ToArray();
        }

        return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyTo<T, TEnumerator>(int count, TEnumerator enumerator, Array array, int index)
        where TEnumerator : struct, IEnumerator<T>
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1 || array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("The destination must be a zero-based, one-dimensional array.", nameof(array));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index > array.Length || count > array.Length - index)
        {
            throw new ArgumentException("The destination array has insufficient space.", nameof(array));
        }

        // Exact element types cannot fail a covariant array store. Keep their hot path
        // separate from exception translation so the JIT can inline the struct iterator.
        if (array.GetType() == typeof(T[]))
        {
            var typed = (T[])array;
            while (enumerator.MoveNext())
            {
                typed[index++] = enumerator.Current;
            }

            enumerator.Dispose();
        }
        else
        {
            CopyToCompatible<T, TEnumerator>(enumerator, array, index);
        }
    }

    private static void CopyToCompatible<T, TEnumerator>(TEnumerator enumerator, Array array, int index)
        where TEnumerator : struct, IEnumerator<T>
    {
        try
        {
            if (array is T[] typed)
            {
                while (enumerator.MoveNext())
                {
                    typed[index++] = enumerator.Current;
                }
            }
            else if (array is object?[] objects)
            {
                while (enumerator.MoveNext())
                {
                    objects[index++] = enumerator.Current;
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
        finally
        {
            enumerator.Dispose();
        }
    }
}
