// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arc.Collections;
using Tinyhand;
using Tinyhand.Resolvers;
using ValueLink;
using ValueLink.Integrality;

namespace NativeAotTest;

// Shared by the native executable and xUnit; no test framework is needed at runtime.
internal static partial class NativeContracts
{
    internal static void TinyhandRegistration()
    {
        LocalGenericRoundTrip(17);
        LocalGenericRoundTrip("anonymous projection");
        var projection = new { Owner = new LocalGenericItem<int>.GoshujinClass { new LocalGenericItem<int> { Value = 23 } } };
        var copy = TinyhandSerializer.Deserialize<LocalGenericItem<int>.GoshujinClass>(TinyhandSerializer.Serialize(projection.Owner))!;
        Check(copy.Single().Value == 23 && copy.Single().Goshujin == copy, "Anonymous projection owner restoration");
    }

    internal static void Serialization()
    {
        // Verify registration before creating any owner or model instance.
        Check(GeneratedResolver.Instance.TryGetFormatter<GenericItem<decimal>.GoshujinClass>() is not null, "Deserialize-first registration");
        GenericRoundTrip(42);
        GenericRoundTrip("generic payload");
        GenericRoundTrip(Guid.Parse("bc463abc-dd10-4ec5-bdfd-d115760f570f"));
        var hidden = new HiddenItem.Owners { new HiddenItem { Id = 3 } };
        var restored = TinyhandSerializer.Deserialize<HiddenItem.Owners>(TinyhandSerializer.Serialize(hidden))!;
        Check(restored.IdChain.FindFirst(3)?.Goshujin == restored, "Private owner and custom owner name");

        var privateArgument = new GenericItem<HiddenItem>.GoshujinClass { new GenericItem<HiddenItem> { Id = 4, Value = new HiddenItem { Id = 5 } } };
        var argumentCopy = TinyhandSerializer.Deserialize<GenericItem<HiddenItem>.GoshujinClass>(TinyhandSerializer.Serialize(privateArgument))!;
        Check(argumentCopy.IdChain.FindFirst(4)?.Value.Id == 5, "Private generic argument");
        var union = new UnionItem.GoshujinClass { new DerivedItem { Id = 6, Text = "derived" } };
        var unionCopy = TinyhandSerializer.Deserialize<UnionItem.GoshujinClass>(TinyhandSerializer.Serialize(union))!;
        Check(unionCopy.IdChain.FindFirst(6) is DerivedItem { Text: "derived" }, "Union owner");
        var nested = new Container { Owners = [new MultiItem.GoshujinClass { new MultiItem { Id = 7 } }] };
        var nestedCopy = TinyhandSerializer.Deserialize<Container>(TinyhandSerializer.Serialize(nested))!;
        Check(nestedCopy.Owners[0].HashChain.FindFirst(7)?.Goshujin == nestedCopy.Owners[0], "Owner collection member");
    }

    internal static void Chains()
    {
        var owner = new MultiItem.GoshujinClass();
        owner.SlidingChain.Resize(8);
        foreach (var id in new[] { 3, 1, 2 })
        {
            var added = new MultiItem { Id = id, Text = "payload" };
            owner.Add(added);
            owner.SlidingChain.Add(added);
        }

        Check(owner.OrderedChain.Keys.SequenceEqual(new[] { 1, 2, 3 }), "Ordered index");
        Check(owner.ReverseChain.Keys.SequenceEqual(new[] { 3, 2, 1 }), "Reverse index");
        Check(owner.QueueChain.Peek().Id == 3 && owner.StackChain.Peek().Id == 2, "Queue and stack");
        var item = owner.HashChain.FindFirst(1)!;
        item.OrderedValue = 4;
        Check(owner.HashChain.FindFirst(1) is null && owner.HashChain.FindFirst(4) == item, "Key update");
        owner.LinkedChain.AddFirst(item);
        owner.ObservableChain.Move(0, 2);
        Check(owner.LinkedChain.First == item && owner.ObservableChain[2].Id == 3, "List moves");
        Check(owner.SlidingChain.Count == 3, "Sliding window");
        var copy = TinyhandSerializer.Deserialize<MultiItem.GoshujinClass>(TinyhandSerializer.Serialize(owner))!;
        Check(copy.ListChain.Select(x => x.Id).SequenceEqual(owner.ListChain.Select(x => x.Id)), "Primary chain round trip");
        Check(copy.LinkedChain.Select(x => x.Id).SequenceEqual(owner.LinkedChain.Select(x => x.Id)), "Linked chain round trip");
        Check(copy.QueueChain.Select(x => x.Id).SequenceEqual(owner.QueueChain.Select(x => x.Id)), "Queue round trip");
        Check(copy.StackChain.Select(x => x.Id).SequenceEqual(owner.StackChain.Select(x => x.Id)), "Stack round trip");
        Check(copy.ObservableChain.Select(x => x.Id).SequenceEqual(owner.ObservableChain.Select(x => x.Id)), "Observable round trip");
        Check(copy.All(x => x.Goshujin == copy && x.Text == "payload"), "Restored ownership");
        var clone = TinyhandSerializer.Clone(owner)!;
        Check(clone.Count == owner.Count && clone.HashChain.FindFirst(4) != item, "Independent clone");
        var other = new MultiItem.GoshujinClass();
        other.SlidingChain.Resize(8);
        item.Goshujin = other;
        Check(owner.Count == 2 && other.Count == 1 && owner.HashChain.FindFirst(4) is null, "Owner transfer");
        other.ClearAll();
        Check(item.Goshujin is null && !item.OrderedLink.IsLinked && !item.SlidingLink.IsLinked, "Owner clear");
    }

    internal static void Isolation()
    {
        var serial = new SerializableItem.GoshujinClass();
        using (serial.LockObject.EnterScope())
        {
            serial.Add(new SerializableItem { Id = 1 });
        }

        Check(serial.GetArray().Length == 1, "Serializable snapshot");
        var copy = TinyhandSerializer.Deserialize<SerializableItem.GoshujinClass>(TinyhandSerializer.Serialize(serial))!;
        Check(copy.GetArray().Single().Goshujin == copy, "Serializable owner round trip");
        var owner = new RepeatableItem.GoshujinClass();
        using (var writer = owner.TryLock(1, AcquisitionMode.CreateOnly)!)
        {
            writer.Value = 10;
            Check(writer.Commit() is not null, "Create transaction");
        }

        var snapshot = owner.TryGet(1)!;
        using (var writer = owner.TryLock(1)!)
        {
            writer.Value = 20;
        }

        Check(owner.TryGet(1) == snapshot && snapshot.Value == 10, "Rollback");
        using (var writer = owner.TryLock(1)!)
        {
            writer.Value = 30;
            Check(writer.Commit() is not null, "Update transaction");
        }

        Check(snapshot.Value == 10 && owner.TryGet(1)!.Value == 30 && owner.SemaphoreCount == 0, "Snapshot isolation and lock release");
        var repeatableCopy = TinyhandSerializer.Deserialize<RepeatableItem.GoshujinClass>(TinyhandSerializer.Serialize(owner))!;
        Check(repeatableCopy.TryGet(1)?.Value == 30, "Repeatable-read owner round trip");
    }

    internal static async Task Synchronization()
    {
        var source = new SyncItem.GoshujinClass { new SyncItem { Id = 1, Revision = 10 } };
        var target = new SyncItem.GoshujinClass();
        var engine = new Integrality<SyncItem.GoshujinClass, SyncItem> { MaxItems = 10, RemoveIfItemNotFound = true };
        var result = await engine.Integrate(target, (request, _) => Task.FromResult(engine.Differentiate(source, request)));
        Check(result.IsSuccess && target.IdChain.FindFirst(1)?.Revision == 10, "Initial synchronization");
        var oldHash = ((IIntegralityObject)source).GetIntegralityHash();
        source.IdChain.FindFirst(1)!.RevisionValue = 20;
        Check(oldHash != ((IIntegralityObject)source).GetIntegralityHash(), "Generated setter invalidates cached hashes");
        result = await engine.Integrate(target, (request, _) => Task.FromResult(engine.Differentiate(source, request)));
        Check(result.IsSuccess && target.IdChain.FindFirst(1)?.Revision == 20, "Synchronize updated content");
        oldHash = ((IIntegralityObject)source).GetIntegralityHash();
        source.IdChain.FindFirst(1)!.PartialRevision = 30;
        Check(oldHash != ((IIntegralityObject)source).GetIntegralityHash(), "Tinyhand partial setter invalidates cached hashes");
        result = await engine.Integrate(target, (request, _) => Task.FromResult(engine.Differentiate(source, request)));
        Check(result.IsSuccess && target.IdChain.FindFirst(1)?.PartialRevision == 30, "Synchronize partial property");
        result = await engine.Integrate(target, (_, _) => Task.FromResult(BytePool.RentArray.CreateFrom(new byte[] { 255, 0 }).AsMemory()));
        Check(result.Result == IntegralityResult.InvalidData && target.Count == 1, "Malformed response");
    }

    private static void GenericRoundTrip<T>(T value)
    {
        // The closed model occurs only after substituting this helper's type argument.
        var owner = new GenericItem<T>.GoshujinClass { new GenericItem<T> { Id = 2, Value = value } };
        var copy = TinyhandSerializer.Deserialize<GenericItem<T>.GoshujinClass>(TinyhandSerializer.Serialize(owner))!;
        Check(EqualityComparer<T>.Default.Equals(copy.IdChain.FindFirst(2)!.Value, value), "Generic helper registration");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
    }

    private static void LocalGenericRoundTrip<T>(T value)
    {
        // Tinyhand must skip the anonymous type and the owner that ValueLink has not generated yet.
        var projection = new { Owner = new LocalGenericItem<T>.GoshujinClass { new LocalGenericItem<T> { Value = value } } };
        var copy = TinyhandSerializer.Deserialize<LocalGenericItem<T>.GoshujinClass>(TinyhandSerializer.Serialize(projection.Owner))!;
        Check(EqualityComparer<T>.Default.Equals(copy.Single().Value, value), "Tinyhand registration with anonymous and unresolved generic types");
        Check(copy.Single().Goshujin == copy, "Local generic owner restoration");
    }

    [TinyhandObject]
    [ValueLinkObject(GoshujinClass = "Owners")]
    private partial class HiddenItem
    {
        [Key(0)]
        [Link(Type = ChainType.Unordered, Primary = true)]
        public int Id { get; set; }
    }
}

/// <summary>Exercises owner types that are unresolved until ValueLink generates them.</summary>
/// <typeparam name="T">The serialized payload type.</typeparam>
[TinyhandObject]
[ValueLinkObject]
public partial class LocalGenericItem<T>
{
    [Key(0)]
    [Link(Type = ChainType.List, Primary = true)]
    public T Value { get; set; } = default!;
}

/// <summary>Exercises every chain type and serialized chain ordering.</summary>
[TinyhandObject]
[ValueLinkObject]
public partial class MultiItem
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Name = "Ordered", AddValue = true)]
    [Link(Type = ChainType.ReverseOrdered, Name = "Reverse")]
    [Link(Type = ChainType.Unordered, Name = "Hash")]
    public int Id { get; set; }

    [Key(1)]
    public string Text { get; set; } = string.Empty;

    [Link(Type = ChainType.List, Name = "List", Primary = true)]
    [Link(Type = ChainType.LinkedList, Name = "Linked")]
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    [Link(Type = ChainType.StackList, Name = "Stack")]
    [Link(Type = ChainType.Observable, Name = "Observable")]
    [Link(Type = ChainType.SlidingList, Name = "Sliding")]
    public MultiItem()
    {
    }

    /// <summary>Makes the owner visible to other generators before ValueLink runs.</summary>
    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
    }
}

/// <summary>Exercises owners stored in a serialized collection.</summary>
[TinyhandObject]
public partial class Container
{
    [Key(0)]
    public List<MultiItem.GoshujinClass> Owners { get; set; } = new();
}

/// <summary>Exercises polymorphic owner serialization.</summary>
[TinyhandUnion(0, typeof(DerivedItem))]
[ValueLinkObject]
public abstract partial class UnionItem
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; set; }
}

/// <summary>Provides a concrete union payload.</summary>
[TinyhandObject]
public partial class DerivedItem : UnionItem
{
    [Key(1)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>Exercises serialization under owner-wide locking.</summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SerializableItem
{
    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true)]
    public int Id { get; set; }
}

/// <summary>Exercises transactional snapshots in native code.</summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableItem
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Primary = true, Unique = true)]
    public int Id { get; private set; }

    [Key(1)]
    public int Value { get; private set; }
}

/// <summary>Exercises synchronization after generated property changes.</summary>
[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SyncItem
{
    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, Unique = true)]
    public int Id { get; set; }

    [Key(1)]
    [Link(Type = ChainType.Ordered, AddValue = true)]
    public int Revision { get; set; }

    [Key(2)]
    [Link(Type = ChainType.Ordered)]
    public partial int PartialRevision { get; set; }
}
