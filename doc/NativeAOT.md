# NativeAOT validation

ValueLink supports NativeAOT through generated, strongly typed owner formatter registration. Generic owners no longer use `MakeGenericType`, `Activator.CreateInstance`, or string-based type lookup. Registration occurs at module initialization, before the first object is created or deserialized.

## Dependency setup

ValueLink references the published **Tinyhand 0.144.1** package. This release fixes static registration of unresolved and anonymous types, including nested generic owner types supplied by ValueLink's generator. Both ordinary builds and NativeAOT use the same package.

```powershell
dotnet restore ValueLink.slnx
```

Use the .NET 10 SDK. The temporary local package, patch, preparation script, and local feed have been removed; standard NuGet restore also works for CI and package consumers.

If you previously built a local package using the same `0.144.1` version, NuGet may reuse it from the global package cache. Check that version's `.nupkg.metadata` source and compare its contents with nuget.org. Move only that cached version aside and restore from nuget.org with `--force --no-http-cache`; those switches alone do not replace a package already present in the global cache.

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
- Anonymous projections and unresolved nested generic owners in source helpers, covering the Tinyhand 0.144.1 registration fixes.
- All chain implementations, indexed updates, ownership transfer, cloning, and serialized chain ordering.
- Serializable owners and RepeatableRead commit, rollback, snapshots, lock release, and serialization.
- Difference synchronization, generated-setter hash invalidation, Tinyhand partial-property hooks, and malformed responses.

The same checks run through xUnit. Generator tests also cover external-only consumers, inferred owner types returned by external factories, inaccessible generic arguments, bounded recursive type expansion, ignored members, and rejection of integrality keys containing managed references. The existing suite covers additional contracts such as ReadCommitted adapters, cancellation, and storage lifecycle behavior; those are not all exercised by the native smoke executable.

## Static registration boundaries

NativeAOT requires the concrete generic types to be known at build time. The generator follows closed type usages, source members, method return types (including external factories), and source generic helper calls. It cannot infer runtime-only type names or generic constructions hidden inside unavailable external method bodies. Add an explicit Tinyhand root for each such closed model:

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
