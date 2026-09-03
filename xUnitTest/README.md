# xUnit tests

The suite uses xUnit v3 and Microsoft.Testing.Platform on .NET 10. Run commands
from the repository root:

```powershell
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --timeout 60s
dotnet test --project xUnitTest/xUnitTest.csproj -c Debug --timeout 60s
dotnet build ValueLink.slnx -c Release
```

For solution-wide test discovery, use `dotnet test --solution ValueLink.slnx`.
Use the `--project` / `--solution` switches required by the runner configured in
`global.json`.

## Coverage map

The tests in `Tests/Coverage` complement the existing integration and regression
tests. This is a functional coverage map, not a claim of 100% line or branch
coverage. Sample applications and benchmarks are checked by the solution build;
external storage engines and performance measurements are outside this test suite.

| Suite | Contracts exercised |
| --- | --- |
| `ChainContractTest` | All eight chain implementations, including forward/reverse ordered variants; regular and `ref Link` add/remove; foreign-owner rejection; enumeration; copying; clearing; reuse; ownership transfer; index updates |
| `ChainBehaviorTest` | Queue/stack ordering and empty operations; bidirectional links; duplicate keys and bounds; list growth and randomized mutations; sliding capacity/holes/positions; collection and property notifications |
| `SerializationContractTest` | Empty, single-item and large Tinyhand round trips; all automatically linked chain types; independent memberships and order; Unicode data; cloning; restored links and ownership |
| `IsolationPrimitiveTest` | Every protection-state transition; concurrent protection; every data-scope result; state forwarding; disposal and deletion; semaphore acquisition and release |
| `ReadCommittedContractTest` | Acquisition modes; missing/existing/obsolete owners; timeout/token/factory forwarding; protected and forced deletion; journal policy; snapshots; store/delete traversal |
| `RepeatableReadContractTest` | Commit and rollback; immutable snapshots; concurrent writers; timeout and cancellation before/during waiting through all three entry points; release lifecycle; recursive deletion |
| `SerializableContractTest` | Store modes and lock scope; failed and throwing storage operations; lock release; snapshots; recursive deletion |
| `IntegralityProtocolTest` | Result packets/counters; truncated and invalid protocol data; hash stack/pool boundaries; exact response size limits; multi-packet convergence; retention policy; broker errors/cancellation; safe reuse of shared responses |
| `GeneratorDiagnosticTest` | Roslyn driver tests for 13 diagnostic IDs; aliases; qualified attributes; nested generics; split partial declarations; partial properties; explicit interface properties; unrelated attributes; recovery after edits; resetting generator options |
| Existing suites | Generic/struct/record/nested types, link sharing, callbacks, property accessibility/notifications, journals, object caching, Tinyhand integration and previously fixed regressions |

Randomized list tests use fixed seeds and report the seed and operation number
when membership diverges. Concurrent tests use explicit synchronization and bounded
waits. They do not depend on network services, sleeps, or performance thresholds.

The generator is both an analyzer and an aliased assembly reference in the test
project. Its runtime reference is needed by Roslyn driver tests; the alias prevents
its embedded enum definitions from conflicting with the library's public types.
Consumer compilations deliberately exclude the generator and test assemblies.

## Regressions detected by the added tests

- `ICollection.CopyTo` on ordered, unordered and sliding chains previously cast
  backing collections to an interface they did not implement.
- Canceled RepeatableRead lock acquisitions leaked the owner's semaphore count.
- RepeatableRead owner deletion cleared chains before capturing child objects.
- Shared integrality result packets incorrectly shared a decrementable reference
  counter across independent callers.
- Explicit interface properties on RepeatableRead records produced invalid writer
  properties in generated code.

Each regression is exercised by the suite alongside its production fix.
