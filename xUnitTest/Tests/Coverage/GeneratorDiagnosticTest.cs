// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

extern alias ValueLinkGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Generator = ValueLinkGenerator::ValueLink.Generator.ValueLinkGeneratorV2;

namespace xUnitTest.Coverage;

/// <summary>
/// Tests generator diagnostics and incremental updates.
/// </summary>
public class GeneratorDiagnosticTest
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);
    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            // The generator embeds enum definitions, so it must not be a reference of the consumer compilation.
            .Where(x => !string.Equals(x, typeof(Generator).Assembly.Location, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, typeof(GeneratorDiagnosticTest).Assembly.Location, StringComparison.OrdinalIgnoreCase))
            .Select(x => MetadataReference.CreateFromFile(x)).ToArray());

    public static IEnumerable<object[]> InvalidDeclarations =>
    [
        ["CLG001", "[ValueLinkObject] public class Item {}"],
        ["CLG002", "public class Outer { [ValueLinkObject] public partial class Item {} }"],
        ["CLG004", "[ValueLinkObject] public partial class Item { public int Goshujin; }"],
        ["CLG005", "[ValueLinkObject] public partial class Item { [Link(Type=ChainType.Ordered, AddValue=true)] public readonly int Id; }"],
        ["CLG008", "[ValueLinkObject] public partial class Item { [Link(Type=ChainType.Ordered, Name=\"Id\")] public Item() {} }"],
        ["CLG010", "[ValueLinkObject] public partial class Item { [Link(Type=ChainType.List)] public Item() {} }"],
        ["CLG019", "[ValueLinkObject] public partial class Item { [Link(Type=ChainType.Ordered, TargetMember=\"Missing\")] public Item() {} }"],
        ["CLG024", "[ValueLinkObject(Isolation=IsolationLevel.RepeatableRead)] public partial class Item { [Link(Type=ChainType.Ordered, Unique=true)] public int Id {get;set;} }"],
        ["CLG026", "[ValueLinkObject(Isolation=IsolationLevel.RepeatableRead)] public partial record Item { public Item(int id) {} [Link(Type=ChainType.Ordered, Unique=true)] public int Id {get;set;} }"],
        ["CLG027", "[ValueLinkObject(Isolation=IsolationLevel.RepeatableRead)] public partial record Item {}"],
        ["CLG028", "[ValueLinkObject] public partial class Item { [Link(Type=ChainType.List, Unique=true)] public int Id {get;set;} }"],
        ["CLG031", "[ValueLinkObject(Integrality=true)] public partial class Item { [Link(Type=ChainType.Ordered, Unique=true)] public string Id {get;set;} = string.Empty; }"],
        ["CLG033", "[ValueLinkObject(Integrality=true)] public partial class Item { [Link(Type=ChainType.Ordered, Unique=true)] public int Id {get;set;} }"],
    ];

    public static IEnumerable<object[]> ValidDeclarations =>
    [
        ["[ValueLink.ValueLinkObjectAttribute] public partial class Item { [Link(Type=ChainType.Ordered, Primary=true)] public int Id {get;set;} }", "Item"],
        ["[Linked] public partial class Item { [Link(Type=ChainType.List, Primary=true)] public int Id {get;set;} }", "Item"],
        ["public partial class Outer<T> { [Linked] public partial class Item { [Link(Type=ChainType.Unordered)] public int Id {get;set;} } }", "Outer`1+Item"],
        ["[Linked] public partial class Item {} public partial class Item { [Link(Type=ChainType.Ordered)] public int Id {get;set;} }", "Item"],
        ["[Linked] public partial class Item { [Link(Type=ChainType.Ordered)] public partial int Id {get;set;} }", "Item"],
        ["public interface IHidden { int Number { get; set; } } [ValueLinkObject(Isolation=IsolationLevel.RepeatableRead)] public partial record Item : IHidden { [Link(Type=ChainType.Ordered, Unique=true, Primary=true)] public int Id {get;private set;} int IHidden.Number {get;set;} }", "Item"],
    ];

    [Theory]
    [MemberData(nameof(InvalidDeclarations))]
    public void InvalidDeclarationsProduceActionableDiagnostics(string expectedId, string declaration)
    {
        var compilation = Compile(declaration);
        var result = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.All(result.Results, x => Assert.Null(x.Exception));
        var diagnostic = Assert.Single(result.Diagnostics.Where(x => x.Id == expectedId));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Same(compilation.SyntaxTrees.Single(), diagnostic.Location.SourceTree);
    }

    [Theory]
    [MemberData(nameof(ValidDeclarations))]
    public void SupportedDeclarationsGenerateCompilableOwners(string declaration, string metadataName)
    {
        var driver = Driver().RunGeneratorsAndUpdateCompilation(Compile(declaration), out var output, out var diagnostics, TestContext.Current.CancellationToken);
        AssertNoErrors(output, diagnostics);
        Assert.All(driver.GetRunResult().Results, x => Assert.Null(x.Exception));
        var item = output.GetTypeByMetadataName(metadataName);
        Assert.NotNull(item);
        var owner = Assert.Single(item.GetTypeMembers("GoshujinClass"));
        Assert.Single(owner.GetMembers("IdChain"));
        Assert.Single(item.GetMembers("Goshujin"));
    }

    [Fact]
    public void UnrelatedAttributesDoNotGenerateValueLinkObjects()
    {
        var compilation = CSharpCompilation.Create("Unrelated", [CSharpSyntaxTree.ParseText("namespace Other; public class ValueLinkObjectAttribute : System.Attribute {} [ValueLinkObject] public class Item {}", ParseOptions, cancellationToken: TestContext.Current.CancellationToken)], References.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ReusedDriverRecoversAfterInvalidSourceIsFixed()
    {
        var invalid = Compile("[Linked] public class Item {}");
        var driver = Driver().RunGenerators(invalid, TestContext.Current.CancellationToken);
        Assert.Contains(driver.GetRunResult().Diagnostics, x => x.Id == "CLG001");
        var valid = Compile("[Linked] public partial class Item { [Link(Type=ChainType.List)] public int Id {get;set;} }");
        driver = driver.RunGeneratorsAndUpdateCompilation(valid, out var output, out var diagnostics, TestContext.Current.CancellationToken);
        AssertNoErrors(output, diagnostics);
        var fresh = Driver().RunGenerators(valid, TestContext.Current.CancellationToken);
        Assert.Equal(GeneratedText(fresh), GeneratedText(driver));
    }

    [Fact]
    public void RemovingGeneratorOptionsDoesNotRetainPreviousNamespace()
    {
        // Tinyhand serialization compilation is covered by SerializationContractTest.
        // Here we inspect only ValueLink's loader symbols, without running Tinyhand's generator.
        const string declaration = "[Tinyhand.TinyhandObject, Linked] public partial class Item { [Tinyhand.Key(0), Link(Type=ChainType.List, Primary=true)] public int Id {get;set;} }";
        var withOptions = Compile(declaration + " [ValueLinkGeneratorOption(CustomNamespace=\"Custom.Loader\")] public class Options {}");
        var driver = Driver().RunGeneratorsAndUpdateCompilation(withOptions, out var customized, out _, TestContext.Current.CancellationToken);
        Assert.NotNull(customized.GetTypeByMetadataName("Custom.Loader.ValueLinkModule"));
        var withoutOptions = Compile(declaration);
        driver = driver.RunGeneratorsAndUpdateCompilation(withoutOptions, out var defaults, out var diagnostics, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(diagnostics, x => x.Severity == DiagnosticSeverity.Error);
        Assert.Null(defaults.GetTypeByMetadataName("Custom.Loader.ValueLinkModule"));
        Assert.NotNull(defaults.GetTypeByMetadataName("ValueLink.ValueLinkModule_GeneratorContract"));
        Assert.Equal(GeneratedText(Driver().RunGenerators(withoutOptions, TestContext.Current.CancellationToken)), GeneratedText(driver));
    }

    [Theory]
    [InlineData("public struct ManagedKey { public string Text; }", "ManagedKey")]
    [InlineData("", "string")]
    public void IntegralityRejectsManagedKeysWithoutAnUnrelatedIsolationError(string keyDeclaration, string keyType)
    {
        var result = Driver().RunGenerators(Compile(keyDeclaration +
            " [Tinyhand.TinyhandObject, ValueLinkObject(Integrality=true, Isolation=IsolationLevel.Serializable)] public partial class Item { " +
            "[Tinyhand.Key(0), Link(Type=ChainType.Unordered, Unique=true, Primary=true)] public " + keyType + " Id {get;set;} }"), TestContext.Current.CancellationToken).GetRunResult();
        Assert.All(result.Results, x => Assert.Null(x.Exception));
        Assert.Single(result.Diagnostics.Where(x => x.Id == "CLG031"));
        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "CLG032");
    }

    [Fact]
    public void ConsumerWithoutLocalModelsRegistersExternalGenericOwners()
    {
        var compilation = Compile("public class Consumer { public NativeAotTest.GenericItem<int>.GoshujinClass Items = new(); }");
        var driver = Driver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);
        AssertNoErrors(output, diagnostics);
        Assert.All(driver.GetRunResult().Results, x => Assert.Null(x.Exception));
        Assert.NotNull(output.GetTypeByMetadataName("ValueLink.ValueLinkModule_GeneratorContract"));
    }

    [Theory]
    [InlineData("public class Consumer { public object Create() => Factory.Create<int>(); }")]
    [InlineData("public class Consumer { public object Create() => Helper<int>(); private static object Helper<T>() => Factory.Create<T>(); }")]
    public void ExternalFactoryReturnTypesRegisterOwners(string declaration)
    {
        // The consumer never spells out the closed owner type, and the factory body is unavailable.
        var factory = Compile("public static class Factory { public static NativeAotTest.GenericItem<T>.GoshujinClass Create<T>() => new(); }")
            .WithAssemblyName("ExternalFactory");
        using var assembly = new MemoryStream();
        var emitted = factory.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var compilation = Compile(declaration).AddReferences(MetadataReference.CreateFromImage(assembly.ToArray()));

        var driver = Driver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);
        AssertNoErrors(output, diagnostics);
        Assert.All(driver.GetRunResult().Results, x => Assert.Null(x.Exception));
        Assert.Contains(GeneratedText(driver), x => x.Contains("GeneratedResolver.RegisterObject<global::NativeAotTest.GenericItem<int>.@GoshujinClass>()", StringComparison.Ordinal));
    }

    [Fact]
    public void PrivateGenericArgumentsRequireAPartialContainingType()
    {
        var result = Driver().RunGenerators(Compile("""
            [Tinyhand.TinyhandObject, Linked] public partial class Item<T> {}
            public class Outer { private class Payload {} private Item<Payload> item = new(); }
            """), TestContext.Current.CancellationToken).GetRunResult();
        Assert.All(result.Results, x => Assert.Null(x.Exception));
        Assert.Single(result.Diagnostics.Where(x => x.Id == "CLG036"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void RecursiveGenericGraphsAreBoundedAndIgnoredMembersAreSkipped(bool ignore, bool linked)
    {
        var declaration = "[Tinyhand.TinyhandObject, Linked] public partial class Trigger {} " +
            (linked ? "[Tinyhand.TinyhandObject, Linked] " : string.Empty) + "public partial class Item<T> { " +
            (ignore ? "[Tinyhand.IgnoreMember] " : string.Empty) +
            "public Item<System.Collections.Generic.List<T>> Next {get;set;} = null!; } public class Root { public Item<int> Item = new(); }";
        var result = Driver().RunGenerators(Compile(declaration), TestContext.Current.CancellationToken).GetRunResult();
        Assert.All(result.Results, x => Assert.Null(x.Exception));
        if (ignore)
        {
            Assert.DoesNotContain(result.Diagnostics, x => x.Severity == DiagnosticSeverity.Error);
        }
        else
        {
            Assert.Single(result.Diagnostics.Where(x => x.Id == "CLG037"));
        }
    }

    private static CSharpCompilation Compile(string declaration) => CSharpCompilation.Create(
        "GeneratorContract",
        [CSharpSyntaxTree.ParseText("using ValueLink; using Linked = ValueLink.ValueLinkObjectAttribute; " + declaration, ParseOptions, "Input.cs")],
        References.Value,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static GeneratorDriver Driver() => CSharpGeneratorDriver.Create([new Generator().AsSourceGenerator()], parseOptions: ParseOptions);

    private static string[] GeneratedText(GeneratorDriver driver) => driver.GetRunResult().Results.SelectMany(x => x.GeneratedSources)
        .OrderBy(x => x.HintName).Select(x => x.HintName + "\n" + x.SourceText.ToString()).ToArray();

    private static void AssertNoErrors(Compilation output, IEnumerable<Diagnostic> diagnostics)
    {
        var errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Concat(diagnostics).Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.True(!errors.Any(), string.Join(Environment.NewLine, errors));
    }
}
