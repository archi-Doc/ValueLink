// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
using NativeAotTest;
using Xunit;

namespace xUnitTest.Coverage;

/// <summary>Runs the native application's contracts under xUnit as regression tests.</summary>
public class NativeAotContractTest
{
    [Fact]
    public void StaticRegistrationSupportsClosedAndInaccessibleOwners() => NativeContracts.Serialization();

    [Fact]
    public void ChainsPreserveIndexesOwnershipAndSerialization() => NativeContracts.Chains();

    [Fact]
    public void IsolatedOwnersPreserveSnapshotsAndSerialization() => NativeContracts.Isolation();

    [Fact]
    public Task GeneratedSettersInvalidateSynchronizationHashes() => NativeContracts.Synchronization();
}
