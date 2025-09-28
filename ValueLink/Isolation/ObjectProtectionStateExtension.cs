// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace ValueLink;

/// <summary>
/// Provides helper methods for manipulating <see cref="ObjectProtectionState"/> values in a thread-safe manner.
/// </summary>
public static class ObjectProtectionStateHelper
{
    /// <summary>
    /// Determines whether the specified <see cref="ObjectProtectionState"/> is obsolete (either <c>Deleted</c> or <c>PendingDeletion</c>).
    /// </summary>
    /// <param name="state">The protection state to check.</param>
    /// <returns><c>true</c> if the state is <c>Deleted</c> or <c>PendingDeletion</c>; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsObsolete(this ObjectProtectionState state)
    {
        return state == ObjectProtectionState.Deleted || state == ObjectProtectionState.PendingDeletion;
    }

    /// <summary>
    /// Attempts to transition the state from <c>Unprotected</c> to <c>Protected</c> atomically.
    /// </summary>
    /// <param name="state">A reference to the state byte to protect.</param>
    /// <returns><c>true</c> if the state was successfully changed to <c>Protected</c>; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryProtect(ref byte state)
    {
        return Interlocked.CompareExchange(ref state, (byte)ObjectProtectionState.Protected, (byte)ObjectProtectionState.Unprotected) == (byte)ObjectProtectionState.Unprotected;
    }

    /// <summary>
    /// Attempts to transition the state from <c>Protected</c> to <c>Unprotected</c> atomically.
    /// </summary>
    /// <param name="state">A reference to the state byte to unprotect.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TryUnprotect(ref byte state)
    {
        Interlocked.CompareExchange(ref state, (byte)ObjectProtectionState.Unprotected, (byte)ObjectProtectionState.Protected);
    }

    /// <summary>
    /// Attempts to mark the state as <c>PendingDeletion</c> if it is currently <c>Protected</c>.
    /// </summary>
    /// <param name="state">A reference to the state byte to mark as pending deletion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TryMarkPendingDeletion(ref byte state)
    {
        Interlocked.CompareExchange(ref state, (byte)ObjectProtectionState.PendingDeletion, (byte)ObjectProtectionState.Protected);
    }

    /// <summary>
    /// Forces the state to <c>Deleted</c> atomically.
    /// </summary>
    /// <param name="state">A reference to the state byte to delete.</param>
    /// <returns><c>true</c> if the state was changed to <c>Deleted</c>; <c>false</c> if it was already <c>Deleted</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ForceDelete(ref byte state)
    {
        return Interlocked.Exchange(ref state, (byte)ObjectProtectionState.Deleted) != (byte)ObjectProtectionState.Deleted;
    }

    /// <summary>
    /// Attempts to transition the state to <c>Deleted</c> atomically, unless the state is <c>Protected</c>.
    /// </summary>
    /// <param name="state">A reference to the state byte to delete.</param>
    /// <param name="originalState">When this method returns, contains the original <see cref="ObjectProtectionState"/> value before deletion.</param>
    /// <returns>
    /// <c>true</c> if the state was successfully changed to <c>Deleted</c>;
    /// <c>false</c> if the state was <c>Protected</c> and could not be deleted.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDelete(ref byte state, out ObjectProtectionState originalState)
    {
        byte byteState;
        do
        {
            byteState = Volatile.Read(ref state);
            if (byteState == (byte)ObjectProtectionState.Protected)
            {// Protected
                originalState = ObjectProtectionState.Protected;
                return false;
            }
        }
        while (Interlocked.CompareExchange(ref state, (byte)ObjectProtectionState.Deleted, (byte)byteState) != byteState);

        originalState = (ObjectProtectionState)byteState;
        return true;
    }
}
