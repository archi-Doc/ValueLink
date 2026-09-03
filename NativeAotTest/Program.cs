// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NativeAotTest;

internal static class Program
{
    private static async Task Main()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Publish and run this test as a NativeAOT executable.");
        }

        NativeContracts.TinyhandRegistration();
        Console.WriteLine("PASS: Tinyhand registration with anonymous and unresolved generic types");
        NativeContracts.Serialization();
        Console.WriteLine("PASS: public, generic, private, union, and nested owner serialization");
        NativeContracts.Chains();
        Console.WriteLine("PASS: all chain types, index updates, ownership, and serialization");
        NativeContracts.Isolation();
        Console.WriteLine("PASS: serializable and repeatable-read isolation");
        await NativeContracts.Synchronization();
        Console.WriteLine("PASS: synchronization, hash invalidation, and malformed packets");
        Console.WriteLine("NativeAOT checks passed.");
    }
}
