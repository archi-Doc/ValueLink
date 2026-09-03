# NativeAOT validation

ValueLink supports NativeAOT through generated, strongly typed owner formatter registration. Generic owners no longer use `MakeGenericType`, `Activator.CreateInstance`, or string-based type lookup. Registration occurs at module initialization, before the first object is created or deserialized.

## Dependency setup

Tinyhand 0.144.0 removed `SetFormatterGenerator`, which ValueLink's previous loader used. Its static registration generator also mistook unresolved and anonymous types for closed types, and attempted to substitute unresolved nested generic types. These errors affect ordinary builds as well as NativeAOT.

This checkout uses **Tinyhand 0.144.1-aotfix.1**, an unpublished local package. Build it before restoring:

```powershell
pwsh -File eng/Prepare-Tinyhand.ps1
dotnet restore ValueLink.slnx
```

The script needs PowerShell 7, Git, the .NET 10 SDK, and network access on its first run. It downloads Tinyhand commit `8670bffa8a605d07491db08d5efb5779cb3c40cd`, recorded in the official 0.144.0 package, applies [the two-line generator fix](Tinyhand-0.144.0-registration.patch), and builds the package under `artifacts/native-aot/packages`. `NuGet.Config` adds that local feed. Existing source checkouts and the published 0.144.0 package are unchanged. Repeated preparation reuses the completed package.

Before releasing ValueLink, release the Tinyhand fix and replace the local dependency version with the published version. Then remove the preparation steps, local feed, and temporary release guard. The publish workflow rejects the local dependency to avoid distributing a package that consumers cannot restore. No package has been published as part of this verification.

## Publish and execute

Install the native toolchain for the target platform using Microsoft's [NativeAOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/). Windows requires the C++ build tools; Linux requires its native compiler and development libraries.

```powershell
dotnet build ValueLink.slnx -c Release
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --timeout 60s
dotnet publish NativeAotTest/NativeAotTest.csproj -c Release -r win-x64 -o artifacts/native-aot/publish
./artifacts/native-aot/publish/NativeAotTest.exe
```

For Linux x64, publish on Linux with `-r linux-x64` and execute `./artifacts/native-aot/publish/NativeAotTest`. The smoke application rejects JIT execution by checking `RuntimeFeature.IsDynamicCodeSupported`. Native compilation treats warnings as errors and roots the ValueLink assembly for analysis without disabling trimming or AOT diagnostics.

Windows x64 publication and execution have been verified locally. Linux x64 is configured in CI but was not executed in the local Windows environment.

## Coverage

- Public, private, custom-named, union, and closed generic owners, including generic models from a separate assembly.
- Generic helper substitution, private generic arguments, registration before construction, and collections of owners.
- All chain implementations, indexed updates, ownership transfer, cloning, and serialized chain ordering.
- Serializable owners and RepeatableRead commit, rollback, snapshots, lock release, and serialization.
- Difference synchronization, generated-setter hash invalidation, Tinyhand partial-property hooks, and malformed responses.

The same checks run through xUnit. Generator tests also cover external-only consumers, inaccessible generic arguments, bounded recursive type expansion, ignored members, and rejection of integrality keys containing managed references. The existing suite covers additional contracts such as ReadCommitted adapters, cancellation, and storage lifecycle behavior; those are not all exercised by the native smoke executable.

## Static registration boundaries

NativeAOT requires the concrete generic types to be known at build time. The generator follows closed type usages, source members, and source generic helper calls. It cannot infer runtime-only type names or generic constructions inside unavailable external method bodies. Add an explicit Tinyhand root for each such closed model:

```csharp
[assembly: Tinyhand.TinyhandRegister(typeof(MyModel<int>))]
```

Private models and private generic arguments require partial containing types so registration bridges can be emitted. `CLG036` identifies inaccessible owners. Recursively expanding models or helpers produce `CLG037` instead of unbounded generation. Reduce the expansion, or mark nonserialized recursive members with `[IgnoreMember]` where appropriate.

Roslyn generators cannot inspect each other's generated source. For an owner used inside another generated type's collection, expose its declaration in the model source:

```csharp
/// <summary>Represents a linked, serialized model.</summary>
[Tinyhand.TinyhandObject]
[ValueLink.ValueLinkObject]
public partial class MyModel
{
    // Add the model's keys and links here.

    /// <summary>Owns this model's objects and supplies generated serialization.</summary>
    [Tinyhand.TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
    }
}
```

This lets Tinyhand register `List<MyModel.GoshujinClass>` while ValueLink supplies the owner implementation. Custom resolvers, third-party formatters, storage adapters, and application reflection must independently support trimming and NativeAOT. Publish and execute the actual application on each supported target.
