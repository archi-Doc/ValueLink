# ValueLink Name Changes

This release renames public APIs of the ValueLink library and the options read by its source generator. Runtime behavior is unchanged except where a table says otherwise.

## How to migrate

1. Update the ValueLink package and rebuild. Generated code (owners, writers, integrality handlers) is regenerated automatically.
2. Apply the whole-word replacements in [Mechanical replacements](#mechanical-replacements).
3. Fix the remaining compile errors with the tables below. The names in [Context-dependent renames](#context-dependent-renames) are too common to replace blindly.
4. Update hand-written implementations of ValueLink interfaces (see [Implementers](#implementers)).

Unchanged: the default generated names (`GoshujinClass`, `Goshujin`, `PropertyChanged`, `NameChain`, `NameLink`, `NameValue`), the generated `StoragePoint` extension methods (`Find`, `TryGet`, `TryLock`, `Delete`), and the wire format of integrality packets.

## Types

| Old | New | Namespace / notes |
| --- | --- | --- |
| `IIntegralityInternal` | `IIntegralityEngine` | `ValueLink.Integrality` |
| `IntegralityState` | `IntegralityPacketType` | `ValueLink.Integrality` |
| `IReadCommittedSemaphore` | `IReadCommittedLockProvider` | `ValueLink` |
| `RepeatableReadExtension` | `RepeatableReadObjectStateExtensions` | `ValueLink`; extension-method call sites are unchanged |
| `ValueLinkGlobal` | `ValueLinkSettings` | `ValueLink` |
| `Arc.Visceral.AutomataKey` | `ValueLink.Internal.AutomataKey` | Namespace change only; used by generated code |
| `<Chain>.ObjectToGoshujinDelegete` | `<Chain>.ObjectToGoshujinDelegate` | Nested delegate in all chain classes |
| `<Chain>.ObjectToLinkDelegete` | `<Chain>.ObjectToLinkDelegate` | Nested delegate in all chain classes |

## Attribute options

| Attribute | Old | New |
| --- | --- | --- |
| `[ValueLinkObject]` | `GoshujinClass` | `GoshujinClassName` |
| `[ValueLinkObject]` | `GoshujinInstance` | `GoshujinPropertyName` |
| `[ValueLinkObject]` | `ExplicitPropertyChanged` | `PropertyChangedEventName` |
| `[Link]` | `AddValue` | `GenerateValue` |

Diagnostic `CLG005` now refers to `GenerateValue`.

## Members

### Chains

| Type | Old | New | Notes |
| --- | --- | --- | --- |
| `SlidingListChain<T>` | `FirstOrDefault` (property) | `First` | |
| `SlidingListChain<T>` | `Consumed` | `UsedSlotCount` | |
| `SlidingListChain<T>` | `Set(int, T)` | `TrySet(int, T)` | |
| `SlidingListChain<T>` | `Get(int)` | `GetOrDefault(int)` | |
| `OrderedChain<TKey, TObj>` | `Reverse` | `IsReversed` | |
| `OrderedChain<TKey, TObj>`, `UnorderedChain<TKey, TObj>` | `KeyObjects` | `KeyObjectPairs` | |
| `UnorderedChain<TKey, TObj>` | `UnsafeGetNodes()` tuple element `Max` | `EndIndex` | Positional deconstruction is unaffected |
| `LinkedListChain<T>` | `void TryAddFirst(T)`, `void TryAddLast(T)` | `bool TryAddFirst(T)`, `bool TryAddLast(T)` | **Signature change**: returns `true` if added, `false` if already linked |
| `QueueListChain<T>` | `void TryEnqueue(T)` | `bool TryEnqueue(T)` | **Signature change**, same semantics |
| `StackListChain<T>` | `void TryPush(T)` | `bool TryPush(T)` | **Signature change**, same semantics |

The `bool` return types are source compatible but binary breaking; recompile dependents.

### Isolation and data access

| Type | Old | New |
| --- | --- | --- |
| `ReadCommittedGoshujin<TKey, TData, TObject, TGoshujin>` | `Find(key, acquisitionMode)` | `GetObject(key, acquisitionMode)` |
| `IRepeatableReadObject<TWriter>` | `TryLockAsyncInternal(...)` (3 overloads) | `TryLockInternalAsync(...)` |
| `IRepeatableReadSemaphore`, `RepeatableReadGoshujin<...>` | `SemaphoreCount` | `AcquisitionCount` |
| `IRepeatableReadSemaphore` | `LockAndForceRelease()` | `LockAndSetReleasing()` |
| `ObjectProtectionStateHelper` | `TryUnprotect(ref byte)` | `Unprotect(ref byte)` |
| `IGoshujin` | `GetEnumerableInternal()` | `EnumerateObjects()` |
| `DataControlState` | `Default` | `None` |
| `DataScopeResult` | `Rip` | `Shutdown` |

### Integrality (`ValueLink.Integrality`)

| Type | Old | New |
| --- | --- | --- |
| `IIntegralityEngine`, `Integrality<TGoshujin, TObject>` | `MaxIntegrationCount` | `MaxIterationCount` |
| `IIntegralityEngine`, `Integrality<TGoshujin, TObject>` | `MaxMemoryLength` | `MaxResponseLength` |
| `IntegralityConstants` | `DefaultMaxIntegrationCount` | `DefaultMaxIterationCount` |
| `IntegralityConstants` | `DefaultMaxMemoryLength` | `DefaultMaxResponseLength` |
| `IntegralityResultHelper` | `ParseMemoryAndResult(rentMemory, out result)` | `ParseResult(rentedMemory, out result)` |
| `IntegralityResultHelper` | `Incomplete` (field) | `IncompleteMemory` |
| `IntegralityResultHelper` | `InvalidData` (field) | `InvalidDataMemory` |

## Parameter renames

These affect only callers that use named arguments.

| Member | Old | New |
| --- | --- | --- |
| `RepeatableReadGoshujin.TryGet/TryLock/TryLockAsync` (`Func` overloads) | `predicate` | `selector` |
| `RepeatableReadGoshujin.TryLock/TryLockAsync` (key overloads) | `mode` | `acquisitionMode` |
| `IntegralityBrokerDelegate`, `IIntegralityGoshujin.Differentiate`, `Integrality.Differentiate` | `integration` | `request` |
| `Integrality.Integrate` | `brokerDelegate` | `broker` |
| `ListChain.Contains`, `ObservableChain.Contains`, `LinkedListChain.Find`, `SlidingListChain.Find` | `value` | `obj` |

## Implementers

Hand-written types that implement ValueLink interfaces must rename these members:

| Interface | Member to rename |
| --- | --- |
| `IGoshujin` | `GetEnumerableInternal()` → `EnumerateObjects()` |
| `IRepeatableReadSemaphore` | `SemaphoreCount` → `AcquisitionCount` |
| `IReadCommittedSemaphore` → `IReadCommittedLockProvider` | Interface name only |
| `IIntegralityInternal` → `IIntegralityEngine` | `MaxIntegrationCount` → `MaxIterationCount`, `MaxMemoryLength` → `MaxResponseLength` |
| `IIntegralityGoshujin` | Parameter `integration` → `request` in `Differentiate` (optional) |

## Mechanical replacements

These whole-word regular expressions are safe to apply across a codebase:

| Find (regex) | Replace |
| --- | --- |
| `\bObjectToGoshujinDelegete\b` | `ObjectToGoshujinDelegate` |
| `\bObjectToLinkDelegete\b` | `ObjectToLinkDelegate` |
| `\bIIntegralityInternal\b` | `IIntegralityEngine` |
| `\bIntegralityState\b` | `IntegralityPacketType` |
| `\bIReadCommittedSemaphore\b` | `IReadCommittedLockProvider` |
| `\bRepeatableReadExtension\b` | `RepeatableReadObjectStateExtensions` |
| `\bValueLinkGlobal\b` | `ValueLinkSettings` |
| `global::Arc\.Visceral\.AutomataKey` | `global::ValueLink.Internal.AutomataKey` |
| `\bTryLockAsyncInternal\b` | `TryLockInternalAsync` |
| `\bSemaphoreCount\b` | `AcquisitionCount` |
| `\bLockAndForceRelease\b` | `LockAndSetReleasing` |
| `\bTryUnprotect\b` | `Unprotect` |
| `\bGetEnumerableInternal\b` | `EnumerateObjects` |
| `\bDefaultMaxIntegrationCount\b` | `DefaultMaxIterationCount` |
| `\bMaxIntegrationCount\b` | `MaxIterationCount` |
| `\bDefaultMaxMemoryLength\b` | `DefaultMaxResponseLength` |
| `\bMaxMemoryLength\b` | `MaxResponseLength` |
| `\bParseMemoryAndResult\b` | `ParseResult` |
| `\bIntegralityResultHelper\.Incomplete\b` | `IntegralityResultHelper.IncompleteMemory` |
| `\bIntegralityResultHelper\.InvalidData\b` | `IntegralityResultHelper.InvalidDataMemory` |
| `\bKeyObjects\b` | `KeyObjectPairs` |
| `\bDataControlState\.Default\b` | `DataControlState.None` |
| `\bDataScopeResult\.Rip\b` | `DataScopeResult.Shutdown` |
| `\bAddValue(\s*=)` | `GenerateValue$1` |
| `\bExplicitPropertyChanged(\s*=)` | `PropertyChangedEventName$1` |
| `\bGoshujinInstance(\s*=\s*")` | `GoshujinPropertyName$1` |

## Context-dependent renames

Review these manually; the old names are common identifiers:

- `GoshujinClass = "..."` inside `[ValueLinkObject(...)]` → `GoshujinClassName = "..."`. Do not rename the generated `GoshujinClass` type.
- `ReadCommittedGoshujin.Find(key, mode)` → `GetObject(key, mode)`.
- `SlidingListChain`: `.Set(` → `.TrySet(`, `.Get(` → `.GetOrDefault(`, `.FirstOrDefault` (property) → `.First`, `.Consumed` → `.UsedSlotCount`.
- `OrderedChain.Reverse` (property) → `IsReversed`.
- `UnsafeGetNodes().Max` → `UnsafeGetNodes().EndIndex`.
- Named arguments listed in [Parameter renames](#parameter-renames).
