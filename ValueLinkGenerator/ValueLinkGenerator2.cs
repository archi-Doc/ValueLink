// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable RS1036

namespace ValueLink.Generator;

/// <summary>
/// Generates ValueLink members incrementally from attributed partial types.
/// </summary>
[Generator]
public class ValueLinkGeneratorV2 : IIncrementalGenerator, IGeneratorInformation
{
    public bool AttachDebugger { get; private set; }

    public bool GenerateToFile { get; private set; }

    public string? CustomNamespace { get; private set; }

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var objects = context.SyntaxProvider.ForAttributeWithMetadataName(
            ValueLinkObjectAttributeMock.FullName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
        var options = context.SyntaxProvider.ForAttributeWithMetadataName(
            ValueLinkGeneratorOptionAttributeMock.FullName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
        var provider = context.CompilationProvider.Combine(objects.Collect().Combine(options.Collect()));

        // Each emission owns its settings, including when the driver is reused after an edit.
        context.RegisterImplementationSourceOutput(provider, static (ctx, source) => new ValueLinkGeneratorV2().Emit(ctx, source));
    }

    private void Emit(SourceProductionContext context, (Compilation Compilation, (ImmutableArray<INamedTypeSymbol> Objects, ImmutableArray<INamedTypeSymbol> Options) Types) source)
    {
        var compilation = source.Compilation;
        this.valueLinkObjectAttributeSymbol = compilation.GetTypeByMetadataName(ValueLinkObjectAttributeMock.FullName);
        if (this.valueLinkObjectAttributeSymbol == null)
        {
            return;
        }

        this.valueLinkGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(ValueLinkGeneratorOptionAttributeMock.FullName);
        if (this.valueLinkGeneratorOptionAttributeSymbol == null)
        {
            return;
        }

        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;

        var body = new ValueLinkBody(context);
        var processed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        this.generatorOptionIsSet = false;
        foreach (var symbol in source.Types.Options)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            this.ProcessSymbol(body, processed, symbol);
        }

        foreach (var symbol in source.Types.Objects)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            this.ProcessSymbol(body, processed, symbol);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Prepare();
        if (body.Abort)
        {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Generate(this, context.CancellationToken);
    }

    private void ProcessSymbol(ValueLinkBody body, HashSet<INamedTypeSymbol> processed, INamedTypeSymbol symbol)
    {
        if (!processed.Add(symbol))
        {
            return;
        }

        foreach (var y in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.valueLinkObjectAttributeSymbol))
            { // ValueLinkObject
                body.Add(symbol);
            }
            else if (!this.generatorOptionIsSet &&
                SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.valueLinkGeneratorOptionAttributeSymbol))
            {
                this.generatorOptionIsSet = true;
                var va = new VisceralAttribute(ValueLinkGeneratorOptionAttributeMock.FullName, y);
                var ta = ValueLinkGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                this.AttachDebugger = ta.AttachDebugger;
                this.GenerateToFile = ta.GenerateToFile;
                this.CustomNamespace = ta.CustomNamespace;
                var path = y.ApplicationSyntaxReference?.SyntaxTree.FilePath;
                this.TargetFolder = string.IsNullOrEmpty(path) ? null : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "Generated");
            }
        }
    }

    private bool generatorOptionIsSet;
    private INamedTypeSymbol? valueLinkObjectAttributeSymbol;
    private INamedTypeSymbol? valueLinkGeneratorOptionAttributeSymbol;
}
