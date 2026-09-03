// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using ValueLink.Integrality;
using Xunit;

namespace xUnitTest.Coverage;

public class IntegralityProtocolTest
{
    public static IEnumerable<object[]> Results => Enum.GetValues<IntegralityResult>().Select(x => new object[] { x });

    [Theory]
    [InlineData(IntegralityResult.Incomplete)]
    [InlineData(IntegralityResult.InvalidData)]
    public void SharedResultPacketsCanBeReturnedByEveryCaller(IntegralityResult expected)
    {
        Parallel.For(0, 100, _ =>
        {
            var packet = expected == IntegralityResult.Incomplete ? IntegralityResultHelper.Incomplete : IntegralityResultHelper.InvalidData;
            try
            {
                IntegralityResultHelper.ParseMemoryAndResult(packet, out var result);
                Assert.Equal(expected, result);
            }
            finally
            {
                packet.Return();
            }
        });
    }

    [Theory]
    [MemberData(nameof(Results))]
    public void ResultPacketsAndCountersRetainTheirMeaning(IntegralityResult expected)
    {
        var memory = BytePool.RentArray.CreateFrom(new[] { (byte)expected }).AsMemory();
        try
        {
            IntegralityResultHelper.ParseMemoryAndResult(memory, out var result);
            Assert.Equal(expected, result);
        }
        finally
        {
            memory.Return();
        }

        var empty = new IntegralityResultAndCount(expected);
        Assert.Equal(expected == IntegralityResult.Success, empty.IsSuccess);
        Assert.False(empty.IsModified);
        Assert.Equal(0, empty.IterationCount);
        Assert.True(new IntegralityResultAndCount(expected, 1, 1, 0).IsModified);
        Assert.True(new IntegralityResultAndCount(expected, 1, 0, 1).IsModified);
        IntegralityResultHelper.ParseMemoryAndResult(default, out var invalid);
        Assert.Equal(IntegralityResult.InvalidData, invalid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public void TruncatedProbeRequestsAreRejected(int length)
    {
        var owner = Create(1);
        var response = Engine().Differentiate(owner, new byte[length]);
        try
        {
            IntegralityResultHelper.ParseMemoryAndResult(response, out var result);
            Assert.Equal(IntegralityResult.InvalidData, result);
            Assert.Single(owner);
        }
        finally
        {
            response.Return();
        }
    }

    [Theory]
    [InlineData(255, 2)]
    [InlineData((byte)IntegralityState.ProbeResponse, 2)]
    [InlineData((byte)IntegralityState.ProbeResponse, 8)]
    public async Task MalformedProbeResponsesDoNotChangeOwnedObjects(byte state, int length)
    {
        var owner = Create(1);
        var previous = owner.IdChain.FindFirst(0);
        var bytes = new byte[length];
        bytes[0] = state;
        var result = await Engine().Integrate(owner, (_, _) => Task.FromResult(BytePool.RentArray.CreateFrom(bytes).AsMemory()), TestContext.Current.CancellationToken);
        Assert.Equal(IntegralityResult.InvalidData, result.Result);
        Assert.False(result.IsModified);
        Assert.Same(previous, Assert.Single(owner));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(341)]
    [InlineData(342)]
    [InlineData(400)]
    public void HashIsStableAcrossCacheInvalidationAndPoolReuse(int count)
    {
        var owner = Create(count);
        var hashOwner = (IIntegralityObject)owner;
        var expected = hashOwner.GetIntegralityHash();
        var rented = ArrayPool<byte>.Shared.Rent((count * 12) + 1);
        Array.Fill(rented, (byte)0xa5);
        ArrayPool<byte>.Shared.Return(rented);
        hashOwner.ClearIntegralityHash();
        Assert.Equal(expected, hashOwner.GetIntegralityHash());
        Assert.Equal(expected, ((IIntegralityObject)Create(count)).GetIntegralityHash());
        var engine = Engine();
        var result = engine.IntegrateForTest(owner, Create(count));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.IterationCount);
        Assert.False(result.IsModified);
    }

    [Fact]
    public void GetResponseHonorsTheExactPacketSizeBoundary()
    {
        var target = Create(1);
        byte[] request = [(byte)IntegralityState.Get, 0, 0, 0, 0];
        var full = Engine().Differentiate(target, request);
        try
        {
            var exact = Engine(full.Length).Differentiate(target, request);
            try
            {
                Assert.Equal(full.Span.ToArray(), exact.Span.ToArray());
            }
            finally
            {
                exact.Return();
            }

            var shortPacket = Engine(full.Length - 1).Differentiate(target, request);
            try
            {
                IntegralityResultHelper.ParseMemoryAndResult(shortPacket, out var result);
                Assert.Equal(IntegralityResult.Incomplete, result);
            }
            finally
            {
                shortPacket.Return();
            }
        }
        finally
        {
            full.Return();
        }
    }

    [Fact]
    public async Task SmallPacketsConvergeOverMultipleIterations()
    {
        var engine = Engine(64);
        var source = Create(0);
        var target = Create(25);
        var calls = 0;
        var result = await engine.Integrate(source, (request, token) =>
        {
            Assert.Equal(TestContext.Current.CancellationToken, token);
            calls++;
            var response = engine.Differentiate(target, request);
            if (request.Span[0] == (byte)IntegralityState.Get)
            {
                Assert.InRange(response.Length, 2, engine.MaxMemoryLength);
            }

            return Task.FromResult(response);
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.True(result.IterationCount > 1);
        Assert.Equal(result.IterationCount + 1, calls);
        Assert.Equal(25, result.IntegratedCount);
        Assert.Equal(target.IdChain.Select(x => (x.Id, x.Name)), source.IdChain.Select(x => (x.Id, x.Name)));
    }

    [Fact]
    public void RetentionPolicyKeepsLocalItemsAndReportsIncompleteWhenHashesDiffer()
    {
        var source = Create(2);
        var target = Create(1);
        var retained = source.IdChain.FindFirst(1);
        var result = Engine(removeMissing: false).IntegrateForTest(source, target);
        Assert.Equal(IntegralityResult.Incomplete, result.Result);
        Assert.Same(retained, source.IdChain.FindFirst(1));
        Assert.Equal(2, source.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrokerFailuresPropagateAndTheOwnerRemainsReusable(bool cancel)
    {
        var engine = Engine();
        var owner = Create(0);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.Integrate(owner, (_, token) => Task.FromCanceled<BytePool.RentMemory>(token), cancellation.Token));
        }
        else
        {
            var exception = new InvalidOperationException("broker failure");
            Assert.Same(exception, await Assert.ThrowsAsync<InvalidOperationException>(() => engine.Integrate(owner, (_, _) => Task.FromException<BytePool.RentMemory>(exception), TestContext.Current.CancellationToken)));
        }

        Assert.Empty(owner);
        var target = Create(3);
        var retry = await engine.Integrate(owner, (request, _) => Task.FromResult(engine.Differentiate(target, request)), TestContext.Current.CancellationToken);
        Assert.True(retry.IsSuccess);
        Assert.Equal(3, owner.Count);
    }

    private static SimpleIntegralityClass.Integrality Engine(int maxMemory = 4096, bool removeMissing = true) => new()
    {
        MaxItems = 500,
        RemoveIfItemNotFound = removeMissing,
        MaxMemoryLength = maxMemory,
        MaxIntegrationCount = 100,
    };

    private static SimpleIntegralityClass.GoshujinClass Create(int count)
    {
        var owner = new SimpleIntegralityClass.GoshujinClass();
        for (var i = 0; i < count; i++)
        {
            owner.Add(new(i, $"value-{i:D3}"));
        }

        return owner;
    }
}
