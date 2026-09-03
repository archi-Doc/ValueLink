// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using ValueLink.Integrality;
using System.Threading.Tasks;
using Tinyhand.Formatters;
using System;
using Arc.Collections;

namespace xUnitTest;

/// <summary>
/// Provides a fixture for tests of incremental synchronization and validation policies.
/// </summary>
[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SimpleIntegralityClass : IEquatableObject
{
    /// <summary>
    /// Provides synchronization policies with bounded item counts for tests.
    /// </summary>
    public class Integrality : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly Integrality Instance10 = new()
        {
            MaxItems = 10,
            RemoveIfItemNotFound = true,
        };

        public static readonly Integrality Instance2 = new()
        {
            MaxItems = 2,
            RemoveIfItemNotFound = true,
        };
    }

    /// <summary>
    /// Rejects items named B during synchronization tests.
    /// </summary>
    public class IntegralityNotB : Integrality<GoshujinClass, SimpleIntegralityClass>
    {
        public static readonly IntegralityNotB Instance = new()
        {
            MaxItems = 10,
            RemoveIfItemNotFound = true,
        };

        public override bool Validate(GoshujinClass goshujin, SimpleIntegralityClass newItem, SimpleIntegralityClass? oldItem)
        {
            if (newItem.Name == "B")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public SimpleIntegralityClass()
    {
    }

    public SimpleIntegralityClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    public bool ObjectEquals(object? other)
    {
        if (other is not SimpleIntegralityClass obj)
        {
            return false;
        }

        return this.Id == obj.Id && this.Name == obj.Name;
    }
}

/// <summary>
/// Provides a fixture for tests of incremental synchronization and validation policies.
/// </summary>
[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class SerializableIntegralityClass : IEquatableObject
{
    public SerializableIntegralityClass()
    {
    }

    public SerializableIntegralityClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    public bool ObjectEquals(object? other)
    {
        if (other is not SerializableIntegralityClass obj)
        {
            return false;
        }

        return this.Id == obj.Id && this.Name == obj.Name;
    }
}

/// <summary>
/// Connects two in-memory owners for synchronization tests.
/// </summary>
public static class IntegralityTestHelper
{
    public static IntegralityResultAndCount IntegrateForTest<TGoshujin, TObject>(this Integrality<TGoshujin, TObject> integrality, TGoshujin goshujin, TGoshujin target)
        where TGoshujin : class, IGoshujin, IIntegralityObject, IIntegralityGoshujin
        where TObject : class, ITinyhandSerializable<TObject>, IIntegralityObject
        => integrality.Integrate(goshujin, (x, y) => Task.FromResult(integrality.Differentiate(target, x))).Result;
}

/// <summary>
/// Tests incremental synchronization and validation policies.
/// </summary>
public class IntegralityTest
{
    [Fact]
    public void IntegrationPreservesAlreadyMatchingObjects()
    {
        var source = new SimpleIntegralityClass.GoshujinClass();
        var target = new SimpleIntegralityClass.GoshujinClass();
        var unchanged = new SimpleIntegralityClass(1, "A");
        source.Add(unchanged);
        source.Add(new(2, "Old"));
        source.Add(new(3, "Removed"));
        target.Add(new(1, "A"));
        target.Add(new(2, "Updated"));
        var result = SimpleIntegralityClass.Integrality.Instance10.IntegrateForTest(source, target);
        Assert.True(result.IsSuccess);
        Assert.Same(unchanged, source.IdChain.FindFirst(1));
        Assert.Null(source.IdChain.FindFirst(3));
        Assert.True(source.ObjectEquals(target));
    }

    [Fact]
    public void ZeroIterationLimitDoesNotReportAnAttempt()
    {
        var engine = new SimpleIntegralityClass.Integrality { MaxItems = 10, RemoveIfItemNotFound = false, MaxIntegrationCount = 0 };
        var target = new SimpleIntegralityClass.GoshujinClass();
        target.Add(new(1, "A"));
        var result = engine.IntegrateForTest(new SimpleIntegralityClass.GoshujinClass(), target);
        Assert.Equal(0, result.IterationCount);
        Assert.Equal(IntegralityResult.Incomplete, result.Result);
    }

    [Theory]
    [InlineData(255)]
    [InlineData((byte)IntegralityState.GetResponse)]
    public async Task InvalidGetResponseIsReportedAsInvalidData(byte state)
    {
        var engine = SimpleIntegralityClass.Integrality.Instance10;
        var target = new SimpleIntegralityClass.GoshujinClass();
        target.Add(new(1, "A"));
        var calls = 0;
        var result = await engine.Integrate(new SimpleIntegralityClass.GoshujinClass(), (packet, _) =>
            Task.FromResult(++calls == 1 ? engine.Differentiate(target, packet) : BytePool.RentArray.CreateFrom(new byte[] { state, 255 }).AsMemory()), TestContext.Current.CancellationToken);
        Assert.Equal(IntegralityResult.InvalidData, result.Result);
    }

    [Fact]
    public void ProbeRespectsMaximumItemCount()
    {
        var engine = SimpleIntegralityClass.Integrality.Instance2;
        var target = new SimpleIntegralityClass.GoshujinClass();
        for (var i = 0; i < 10; i++)
        {
            target.Add(new(i, "A"));
        }

        byte[] probe = new byte[1 + sizeof(ulong)];
        probe[0] = (byte)IntegralityState.Probe;
        var packet = engine.Differentiate(target, probe);
        try
        {
            Assert.Equal(1 + sizeof(ulong) + (2 * (sizeof(int) + sizeof(ulong))), packet.Length);
        }
        finally
        {
            packet.Return();
        }
    }

    [Fact]
    public void ClearChainsInvalidatesTheCachedHash()
    {
        var owner = new SimpleIntegralityClass.GoshujinClass();
        owner.Add(new(1, "A"));
        var hashObject = (IIntegralityObject)owner;
        var previousHash = hashObject.GetIntegralityHash();
        owner.ClearChains();
        Assert.NotEqual(previousHash, hashObject.GetIntegralityHash());
        Assert.Equal(((IIntegralityObject)new SimpleIntegralityClass.GoshujinClass()).GetIntegralityHash(), hashObject.GetIntegralityHash());
    }

    [Fact]
    public void IntegrateObjectAcquiresTheSerializableLock()
    {
        var engine = new LockCheckingIntegrality { MaxItems = 10, RemoveIfItemNotFound = true };
        var owner = new SerializableIntegralityClass.GoshujinClass();
        Assert.Equal(IntegralityResult.Success, engine.IntegrateObject(owner, new(1, "A")));
    }

    private sealed class LockCheckingIntegrality : Integrality<SerializableIntegralityClass.GoshujinClass, SerializableIntegralityClass>
    {
        public override bool Validate(SerializableIntegralityClass.GoshujinClass goshujin, SerializableIntegralityClass newItem, SerializableIntegralityClass? oldItem)
        {
            Assert.True(goshujin.LockObject.IsLocked);
            return true;
        }
    }

    [Fact]
    public async Task SerializableIntegrationDoesNotReenterItsSemaphore()
    {
        var engine = new LockCheckingIntegrality { MaxItems = 10, RemoveIfItemNotFound = true };
        var source = new SerializableIntegralityClass.GoshujinClass();
        var target = new SerializableIntegralityClass.GoshujinClass();
        target.Add(new(1, "A"));
        var result = await Task.Run(() => engine.IntegrateForTest(source, target), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.True(source.ObjectEquals(target));
    }

    [Fact]
    public void Test1()
    {
        SimpleIntegralityClass.GoshujinClass g;
        SimpleIntegralityClass.GoshujinClass g2;

        g = new(); // 1, 2, 3
        g.Add(new(1, "A"));
        g.Add(new(2, "B"));
        g.Add(new(3, "C"));

        g2 = new(); // 1, 2, 3
        g2.ObjectEquals(g).IsFalse();

        var resultAndCount = SimpleIntegralityClass.Integrality.Instance10.IntegrateForTest(g2, g);
        resultAndCount.Result.Is(IntegralityResult.Success);
        g2.ObjectEquals(g).IsTrue();

        g2 = new(); // 1, 2
        resultAndCount = SimpleIntegralityClass.Integrality.Instance2.IntegrateForTest(g2, g);
        resultAndCount.Result.Is(IntegralityResult.Incomplete);
        g2.IdChain.FindFirst(1).IsNotNull();
        g2.IdChain.FindFirst(2).IsNotNull();
        g2.IdChain.FindFirst(3).IsNull();
        g2.ObjectEquals(g).IsFalse();

        g2 = new(); // 1, 3
        resultAndCount = SimpleIntegralityClass.IntegralityNotB.Instance.IntegrateForTest(g2, g);
        resultAndCount.Result.Is(IntegralityResult.Incomplete);
        g2.IdChain.FindFirst(1).IsNotNull();
        g2.IdChain.FindFirst(2).IsNull();
        g2.IdChain.FindFirst(3).IsNotNull();
        g2.ObjectEquals(g).IsFalse();

        g2 = new();
        g2.ObjectEquals(g).IsFalse();

        resultAndCount = SimpleIntegralityClass.Integrality.Instance10.IntegrateForTest(g, g2);
        resultAndCount.Result.Is(IntegralityResult.Success);
        Assert.Empty(g);
    }
}
