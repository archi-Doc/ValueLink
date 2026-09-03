# ValueLink

![NuGet](https://img.shields.io/nuget/v/ValueLink) ![Build and Test](https://github.com/archi-Doc/ValueLink/workflows/Build%20and%20Test/badge.svg)

ValueLink is a C# source generator and collection library for managing objects through multiple indexes and orderings. Each object stores its own links; a generated **Goshujin** (owner) maintains the corresponding **chains**. Generated setters keep indexes in sync with value changes.

Features include ordered and hash indexes, lists, queues, stacks, observable collections, bounded sliding windows, Tinyhand serialization, configurable isolation, and incremental synchronization.

[Japanese guide](doc/README.jp.md)

## Contents

- [Requirements and installation](#requirements-and-installation)
- [Quick start](#quick-start)
- [Ownership and generated members](#ownership-and-generated-members)
- [Chains](#chains)
- [Attribute options](#attribute-options)
- [Notifications and callbacks](#notifications-and-callbacks)
- [Serialization and journaling](#serialization-and-journaling)
- [NativeAOT](#nativeaot)
- [Isolation](#isolation)
- [Incremental synchronization](#incremental-synchronization)
- [Performance](#performance)
- [Build and tests](#build-and-tests)

## Requirements and installation

Use the .NET 10 SDK or later, a `net10.0` or later target, and C# 14 or later. The NuGet package includes the source generator.

```shell
dotnet add package ValueLink
```

Declare linked models as `partial` classes or record classes and annotate them with `[ValueLinkObject]`. Nested models require partial containing types. Generic models are supported. A linked model may inherit ordinary members, but inheriting from another `[ValueLinkObject]` model is not supported.

## Quick start

This complete console example indexes the same people by ID and age. `AddValue = true` requests a generated setter; it is **false by default**.

```csharp
using System;
using ValueLink;

var people = new Person.GoshujinClass();
var ada = new Person(1, "Ada", 30);
people.Add(ada);
people.Add(new Person(2, "Bea", 20));

Console.WriteLine(people.IdChain.FindFirst(1)?.Name); // Ada
ada.AgeValue = 18; // Updates AgeChain as well as the underlying field.

foreach (var person in people.AgeChain)
{
    Console.WriteLine($"{person.Name}: {person.AgeValue}");
}
// Ada: 18
// Bea: 20

people.Remove(ada); // Removes all of Ada's links and clears her owner.
Console.WriteLine(ada.Goshujin is null); // True

/// <summary>Represents a person indexed by ID and age.</summary>
[ValueLinkObject]
public partial class Person
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; private set; }

    public string Name { get; private set; }

    [Link(Type = ChainType.Ordered, AddValue = true,
        Accessibility = ValueLinkAccessibility.Public)]
    private int age;

    public Person(int id, string name, int age)
    {
        this.Id = id;
        this.Name = name;
        this.age = age;
    }
}
```

## Ownership and generated members

By default, the generator adds a nested `GoshujinClass`, an object's `Goshujin` property, and one `NameChain`/`NameLink` pair per link. A field named `age` produces `AgeChain`, `AgeLink`, and, when requested, `AgeValue`.

For `None` and `Serializable` isolation, assigning `obj.Goshujin = owner` or calling `owner.Add(obj)` transfers ownership and updates automatic links. Assigning `null` or calling `owner.Remove(obj)` removes the object from all chains. Each object has one owner at a time.

Direct chain operations change only that chain's membership. Assign the owner first; inserting an object into another owner's chain throws `UnmatchedGoshujinException`. Link fields contain collection state and should not be copied or edited directly. Likewise, copying a linked record with `with` does not create an independently linked object; use a RepeatableRead writer for record updates.

Set `Primary = true` on a chain that contains every owned object. It supplies the owner's `Count`, enumeration order, and serialization order. ReadCommitted owners expose their contents through operations such as `ForEach` and `GetArray`, rather than `IEnumerable<T>`.

| Operation | Effect |
| --- | --- |
| `owner.SomeChain.Clear()` | Clears that chain; preserves owner references and other chains |
| `owner.ClearChains()` | Clears all chains; preserves object owner references |
| `owner.ClearAll()` | Removes objects found in the primary chain from all chains and clears their owner references |

`ClearAll` is generated as a public operation only for `None` or `Serializable` owners with a primary chain. Other configurations throw `NotImplementedException` through `IGoshujin.ClearAll`. Acquire the owner lock when clearing synchronized owners. Materialize a snapshot before mutating a chain during enumeration.

Use generated value properties or partial properties to change indexed values. Writing a backing field or an ordinary property directly bypasses index maintenance and notifications.

### Partial properties

A partial property can provide the public API directly, without a separate `Value` suffix:

```csharp
using ValueLink;

/// <summary>Represents an item with an automatically maintained ID index.</summary>
[ValueLinkObject]
public partial class IndexedItem
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    public partial int Id { get; set; }
}
```

## Chains

`n` is the number of linked objects. Costs below refer to direct operations on one chain, without callbacks or owner-wide updates. No chain provides synchronization by itself.

| `ChainType` | Collection | Main operations and costs |
| --- | --- | --- |
| `List` | `ListChain<T>` | Index, membership, and removal: O(1). Add/insert: amortized O(1). Removal fills the gap with the last object; insertion may move the displaced object to the end. |
| `LinkedList` | `LinkedListChain<T>` | Add, move, remove by object, and link navigation: O(1). Preserves explicit list order. |
| `StackList` | `StackListChain<T>` | Push, peek, pop, and remove by object: O(1). Enumeration runs from bottom to top. |
| `QueueList` | `QueueListChain<T>` | Enqueue, peek, dequeue, and remove by object: O(1). Enumeration follows dequeue order. |
| `Ordered` | `OrderedChain<TKey, TObject>` | Key search, add, remove, and bounds: O(log n). Ascending key order; duplicate keys allowed. |
| `ReverseOrdered` | `OrderedChain<TKey, TObject>` | The same ordered chain with reversed comparison and traversal. |
| `Unordered` | `UnorderedChain<TKey, TObject>` | Hash lookup, add, and removal: expected O(1), worst-case O(n). Duplicate keys allowed; enumeration is unsorted. |
| `Observable` | `ObservableChain<T>` | Index: O(1). Append: amortized O(1); moving an existing object, search, insertion, and removal: O(n). Raises collection-change notifications. |
| `SlidingList` | `SlidingListChain<T>` | Bounded circular window. Position lookup and append: O(1). Head removal can scan holes; resizing and enumeration scale with window capacity. |
| `None` | No collection | Generates value/notification support without a chain. |

Enumeration takes O(n) for ordinary lists and trees. Hash chains may scan unused backing slots; sliding chains may scan empty window positions. Constructing an enumerator is not the same cost as traversing the collection.

Ordered and unordered chains provide `FindFirst`, `ContainsKey`, `TryGetValue`, `Keys`, `Objects`, and `KeyObjects`. Use ordered `FindAll(key)` or unordered `Enumerate(key)` to enumerate duplicate keys. A missing `FindFirst` result is `null` for class models. Ordered chains also expose `GetLowerBound`, `GetUpperBound`, and inclusive `GetRange`; bounds follow the configured comparison order. Links on linked and ordered chains allow neighbor navigation.

### Sliding windows

Sliding chains start with zero capacity and always require manual insertion, regardless of `AutoLink`:

```csharp
using System;
using ValueLink;

var owner = new WindowItem.GoshujinClass();
owner.WindowChain.Resize(4);
var item = new WindowItem { Goshujin = owner };
Console.WriteLine(owner.WindowChain.Add(item)); // True
int position = item.WindowLink.Position;
Console.WriteLine(owner.WindowChain.Get(position) == item); // True

/// <summary>Represents an object placed manually in a bounded window.</summary>
[ValueLinkObject]
public partial class WindowItem
{
    [Link(Type = ChainType.SlidingList, Name = "Window")]
    public WindowItem() { }
}
```

`Count` counts live objects; `Consumed` also counts holes left by removal. `StartPosition` is the logical window start, and `EndPosition` is exclusive. Positions wrap within the underlying integer range. `Add` returns `false` when full or already linked. `Set(position, obj)` can replace an entry within the capacity window and unlinks the displaced object. `Resize` preserves logical positions and returns `false` if the new capacity is smaller than `Consumed`.

## Attribute options

### `ValueLinkObject`

| Option | Default | Purpose |
| --- | --- | --- |
| `GoshujinClass` | `"GoshujinClass"` | Generated owner type name |
| `GoshujinInstance` | `"Goshujin"` | Generated owner property name |
| `ExplicitPropertyChanged` | `"PropertyChanged"` | Event name used for generated notifications |
| `Isolation` | `None` | Concurrency model described below |
| `Restricted` | `false` | Makes the owner property internal and defaults links to private access with `AddValue = false`; explicit link options can override those defaults |
| `Integrality` | `false` | Enables hash-based difference synchronization |

Empty name options select the defaults above.

### `Link`

| Option | Default | Purpose |
| --- | --- | --- |
| `Type` | `None` | Chain implementation |
| `Name` | Target member name, initial letter capitalized | Prefix for chain, link, and generated value names; specify distinct names for multiple indexes on one member |
| `Primary` | `false` | Selects the chain used for owner enumeration and serialization |
| `Unique` | `false` | Identifies the key used by isolation, journaling, and synchronization |
| `AddValue` | `false` | Generates a property that updates links when the value changes |
| `AutoLink` | `true` | Adds the link when ownership is assigned; ignored for sliding insertion |
| `AutoNotify` | `false` | Raises `PropertyChanged` from generated value changes |
| `Accessibility` | `PublicGetter` | Controls generated value/link access |
| `TargetMember` | Empty | Selects an accessible field or property for a constructor-level link |
| `UnsafeTargetChain` | Empty | Shares another chain instead of creating a new chain |

`Unique` does not turn the underlying chain into a duplicate-rejecting collection. RepeatableRead commits check the selected unique key; callers that manipulate raw chains must preserve their own uniqueness invariant.

`PublicGetter` gives the generated value a public getter and inherits setter access from the target. Use `Public` to expose both accessors, or `Protected`, `Private`, or `Inherit` as needed. Getter-only targets do not receive writable value properties. Partial properties retain their declared accessor visibility.

With `AutoLink = false`, manually choose initial membership after assigning the owner. A generated value change can still add or update that link. Constructor attributes can define unkeyed chains or index a member using `TargetMember`. `UnsafeTargetChain` requires compatible key/object types and the correct `ref obj.SomeLink` argument for each shared entry; see [TargetChainTest](xUnitTest/Tests/TargetChainTest.cs).

### Generator options

Apply `[ValueLinkGeneratorOption]` to a class to set `GenerateToFile` or `CustomNamespace`. `GenerateToFile = true` writes source into an existing `Generated` folder beside that file; otherwise the compiler receives the generated source normally. `CustomNamespace` changes the module initializer namespace, not model namespaces. `AttachDebugger` is currently a reserved option with no effect.

## Notifications and callbacks

Use `AutoNotify = true` with `AddValue = true` to generate `INotifyPropertyChanged` support. If the model already provides the event, the generator uses it. `ExplicitPropertyChanged` selects a custom event name. Value equality suppresses redundant setter updates and notifications.

`ObservableChain<T>` separately implements `INotifyCollectionChanged` and `INotifyPropertyChanged` for collection changes. Its notifications run on the calling thread; UI applications must perform mutations on the appropriate thread. It identifies reference-type objects by identity, so distinct equal-valued records can coexist.

For a link named `Age`, the generator recognizes these optional instance methods:

```csharp
private bool AgeLinkPredicate() => this.age >= 18;
private void AgeLinkAdded() { /* React to generated insertion. */ }
private void AgeLinkRemoved() { /* React to generated removal. */ }
```

The predicate controls generated insertion. Added/removed callbacks accompany generated link operations; direct calls to a chain bypass these model callbacks. Keep them short and account for the calling operation's lock scope. See [AdditionalMethodTest](xUnitTest/Tests/AdditionalMethodTest.cs) and [NotifyPropertyChangedTest](xUnitTest/Tests/NotifyPropertyChangedTest.cs).

## Serialization and journaling

Add `[TinyhandObject]` and `[Key]` attributes to serialize models and their generated owners with [Tinyhand](https://github.com/archi-Doc/Tinyhand). Choose a primary chain that contains all objects. The generated owner serializer preserves supported chain memberships and ordering and restores object ownership.

```csharp
using System;
using Tinyhand;
using ValueLink;

var original = new StoredItem.GoshujinClass();
original.Add(new StoredItem { Id = 1, Name = "Ada" });
byte[] bytes = TinyhandSerializer.Serialize(original);
var restored = TinyhandSerializer.Deserialize<StoredItem.GoshujinClass>(bytes)!;
Console.WriteLine(restored.IdChain.FindFirst(1)?.Name); // Ada

/// <summary>Represents a serializable item indexed by ID.</summary>
[TinyhandObject]
[ValueLinkObject]
public partial class StoredItem
{
    [Key(0)]
    [Link(Type = ChainType.Ordered, Primary = true)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}
```

Initialize ordinary properties before adding objects; use generated setters for later indexed changes. Treat sliding-window capacity and membership as runtime configuration to restore explicitly. See [SerializationContractTest](xUnitTest/Tests/Coverage/SerializationContractTest.cs) for round trips, cloning, and independent memberships.

`[TinyhandObject(Structural = true)]` enables structural/journal integration. Use a primary chain and a unique keyed link, then connect the owner to an `IStructuralRoot` implementation. Generated operations can record changes, but storage, journal persistence, and replay are supplied by the host application. Serializing an owner alone does not provide durable storage. See [JournalTest](xUnitTest/Tests/JournalTest.cs).

## NativeAOT

ValueLink generates static Tinyhand formatter registrations for owners, including closed generic models, private nested models, and unions. It does not construct owner formatters through runtime reflection. The library enables .NET trimming and AOT analyzers with `IsAotCompatible`.

This checkout requires a local Tinyhand registration fix. Run `pwsh -File eng/Prepare-Tinyhand.ps1` before restoring or building; see [NativeAOT setup and limitations](doc/NativeAOT.md). The script builds an unpublished package from pinned upstream source plus the included patch. A released Tinyhand dependency is required before publishing ValueLink.

On a host with the [NativeAOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/), publish and run the smoke test:

```powershell
dotnet publish NativeAotTest/NativeAotTest.csproj -c Release -r win-x64 -o artifacts/native-aot/publish
./artifacts/native-aot/publish/NativeAotTest.exe
```

Windows x64 has been verified locally. The CI workflow also includes Linux x64; use `linux-x64` and run the executable without `.exe` on Linux. Always test the published executable; `dotnet test` alone verifies JIT execution.

Closed generic types must be discoverable at compilation time. Explicit `[assembly: TinyhandRegister(typeof(MyModel<int>))]` roots cover types used only through external generic helpers. Containing types needed to access private generic arguments must be partial. When another generated model stores a collection of owners, declare the owner as a partial class with `[TinyhandObject(External = true)]` so Tinyhand can resolve its type before ValueLink runs; see [NativeContracts](NativeAotTest/NativeContracts.cs).

## Isolation

| Mode | Model |
| --- | --- |
| `None` | Caller controls all synchronization |
| `Serializable` | Generated owner provides `LockObject`; callers lock compound reads and mutations |
| `ReadCommitted` | Keyed owner delegates data retrieval and locking to `IDataLocker<TData>` adapters |
| `RepeatableRead` | Record classes publish updated copies through exclusive writers |

### Serializable

Protect collection operations and indexed mutations with the owner's lock. For a model configured with `Isolation = IsolationLevel.Serializable`:

```csharp
using (owner.LockObject.EnterScope())
{
    // Read or mutate this owner's chains and objects here.
}
```

`GetArray()` takes a snapshot under the lock and copies object references. The Serializable `LockObject` is a non-reentrant semaphore: call `GetArray()` outside an already-held owner lock. Storage lifecycle operations also synchronize their work. Coordinate both owners when transferring objects between them.

### ReadCommitted

Configure a unique key and implement `IDataLocker<TData>` on the linked adapter. The generated owner supplies `Find`, `TryGet`, `TryLock`, `TryDelete`, `ForEach`, and `GetArray`. Timeouts, cancellation tokens, and optional factories are forwarded to the adapter, which implements data storage and locking.

`TryLock` returns `ValueTask<DataScope<TData>>`. Inspect `Result` for `Retrieved`, `Created`, or a failure, and dispose the scope to release its lock. Do not dispose multiple copies of this mutable struct. For value-type data, `IsValid` is only a non-null check and does not prove acquisition succeeded; use `Result`. `UnlockAndDelete` releases the lock and requests deletion through the associated storage object.

Protected objects delay deletion unless forced. Store/release and delete operations propagate through Tinyhand structural objects. See [ReadCommittedContractTest](xUnitTest/Tests/Coverage/ReadCommittedContractTest.cs) for an in-memory adapter and lifecycle examples.

### RepeatableRead

Use a partial record class with a unique keyed link. Acquire a writer, change its properties, call `Commit`, and dispose it. `Commit` returns the published record, or `null` if it cannot publish, such as on a unique-key conflict. Disposing without a commit discards unpublished edits.

```csharp
using System;
using ValueLink;

var accounts = new Account.GoshujinClass();
using (var writer = await accounts.TryLockAsync(1, AcquisitionMode.GetOrCreate))
{
    if (writer is null)
    {
        throw new InvalidOperationException("The account could not be acquired.");
    }

    writer.Balance = 100;
    Account published = writer.Commit()
        ?? throw new InvalidOperationException("The account could not be committed.");
    Console.WriteLine(published.Balance); // 100
}

/// <summary>Represents an account updated through a repeatable-read writer.</summary>
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record Account
{
    [Link(Type = ChainType.Unordered, Primary = true, Unique = true)]
    public int Id { get; private set; }

    public int Balance { get; private set; }
}
```

Readers retain the record version they acquired. Record copies are shallow: clone a mutable list, array, or nested object before changing it through the writer. Do not mutate a published record or a shared reference directly.

`TryGet` retrieves a record. `TryLock` waits synchronously for a writer; `TryLockAsync` supports a timeout and cancellation token. A timeout returns `null`; cancellation while waiting propagates as `OperationCanceledException`. Overloads without a timeout use `ValueLinkGlobal.LockTimeout`. Direct chain reads still require the owner lock or a snapshot.

Both keyed isolation modes use `AcquisitionMode.GetOnly`, `GetOrCreate`, and `CreateOnly`. `GetOnlyIgnoreState` requests adapter-specific state handling; it does not bypass an invalid owner or create missing data.

## Incremental synchronization

**Integrality** compares cached hashes, requests differing objects, and integrates responses. Enable `[ValueLinkObject(Integrality = true)]` together with `[TinyhandObject]`, a unique unmanaged struct key such as `int`, and `None` or `Serializable` isolation. Structs containing managed references are rejected because keys are copied as raw bytes.

This local broker demonstrates the request/response contract; replace it with your transport as needed:

```csharp
using System;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;
using ValueLink.Integrality;

var source = new SyncItem.GoshujinClass();
var target = new SyncItem.GoshujinClass();
source.Add(new SyncItem { Id = 1, Name = "Ada" });
var engine = new Integrality<SyncItem.GoshujinClass, SyncItem>
{
    MaxItems = 1_000,
    RemoveIfItemNotFound = true,
};

var result = await engine.Integrate(target, (request, cancellationToken) =>
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(engine.Differentiate(source, request));
});
Console.WriteLine(result.Result); // Success

/// <summary>Represents an item synchronized by a unique ID.</summary>
[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SyncItem
{
    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, Unique = true)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}
```

`MaxItems` limits reported keys and new items; `RemoveIfItemNotFound` removes local entries absent from the remote key list. `MaxMemoryLength` limits object-response packets, not probe responses. `MaxIntegrationCount` limits object-request iterations after probing. Override `Validate` to accept/reject incoming objects and `Trim` for application-specific removal; the default `Trim` removes nothing.

The broker transfers ownership of its returned `BytePool.RentMemory` to the engine. When calling `Differentiate` outside a broker, return that buffer after use. Request bytes are valid only until the broker task completes.

Integration may make partial changes before returning an incomplete/error result. Broker exceptions and cancellation propagate. Serialize concurrent runs targeting the same owner. Reported counts cover integration and trimming, but exclude removals during key comparison; `IsModified` is not a complete change log. Generated link setters, including Tinyhand partial-property update hooks, invalidate both object and owner hashes. After directly changing serialized fields, ordinary properties, or nested mutable content, call `((IIntegralityObject)item).ClearIntegralityHash()` yourself.

## Performance

Choose chains for the operations you need and add only the indexes you use. Embedded links avoid searching for an object's node when removing it. Each additional chain consumes memory and adds work to ownership changes or indexed updates. Record writers allocate copies, and callbacks or serialization can dominate collection costs.

Measure against representative workloads using the benchmarks in this repository:

```shell
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*ChainMaintenanceBenchmark*"
```

Use Release builds and compare both timings and allocations. Results depend on runtime, hardware, collection size, key distribution, and enabled features.

## Build and tests

Run from the repository root:

```shell
pwsh -File eng/Prepare-Tinyhand.ps1
dotnet restore ValueLink.slnx
dotnet build ValueLink.slnx -c Release --no-restore
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --timeout 60s
dotnet test --solution ValueLink.slnx -c Debug --timeout 60s
```

The suite uses xUnit v3 and Microsoft.Testing.Platform, configured in `global.json`; use the `--project` or `--solution` switch. It exercises chains, ownership, serialization, isolation, synchronization, and source generation. See the [test coverage map](xUnitTest/README.md) for contracts and limitations.

| Project | Purpose |
| --- | --- |
| `ValueLink` | Public attributes, chains, isolation, and synchronization APIs |
| `ValueLinkGenerator` | Roslyn source generator |
| `xUnitTest` | Contract, integration, and regression tests |
| `NativeAotTest` | Native executable sharing behavioral checks with xUnit |
| `NativeAotModels` | Cross-assembly generic fixtures for native and xUnit checks |
| `QuickStart` | Runnable usage examples |
| `Playground` | Development experiments and storage adapter examples |
| `Benchmark` | BenchmarkDotNet performance experiments |

ValueLink is distributed under the [MIT license](LICENSE).
