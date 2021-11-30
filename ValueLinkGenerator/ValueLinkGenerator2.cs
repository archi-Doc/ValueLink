// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace ValueLink.Generator
{
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
            var provider = context.CompilationProvider.Combine(
                context.SyntaxProvider
                .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Collect());

            context.RegisterImplementationSourceOutput(provider, this.Emit);
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            if (node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0)
            {
                return true;
            }
            else if (node is GenericNameSyntax { })
            {
                return false; // true;
            }
            else
            {
                return false;
            }
        }

        private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (context.Node is TypeDeclarationSyntax typeSyntax)
            {
                foreach (var attributeList in typeSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (name.EndsWith(ValueLinkGeneratorOptionAttributeMock.StandardName) ||
                            name.EndsWith(ValueLinkGeneratorOptionAttributeMock.SimpleName))
                        {
                            return typeSyntax;
                        }
                        else if (name.EndsWith(ValueLinkObjectAttributeMock.StandardName) ||
                            name.EndsWith(ValueLinkObjectAttributeMock.SimpleName))
                        {
                            return typeSyntax;
                        }
                    }
                }
            }

            return null;
        }

        private void Emit(SourceProductionContext context, (Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> types) source)
        {
            var compilation = source.compilation;
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
            // receiver.Generics.Prepare(compilation);
#pragma warning disable RS1024 // Symbols should be compared for equality
            var processed = new HashSet<INamedTypeSymbol?>();
#pragma warning restore RS1024 // Symbols should be compared for equality

            this.generatorOptionIsSet = false;
            foreach (var x in source.types)
            {
                if (x == null)
                {
                    continue;
                }

                context.CancellationToken.ThrowIfCancellationRequested();

                var model = compilation.GetSemanticModel(x.SyntaxTree);
                if (model.GetDeclaredSymbol(x) is INamedTypeSymbol symbol)
                {
                    this.ProcessSymbol(body, processed, x.SyntaxTree, symbol);
                }
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

        private void ProcessSymbol(ValueLinkBody body, HashSet<INamedTypeSymbol?> processed, SyntaxTree syntaxTree, INamedTypeSymbol symbol)
        {
            if (processed.Contains(symbol))
            {
                return;
            }

            processed.Add(symbol);
            foreach (var y in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, this.valueLinkObjectAttributeSymbol))
                { // ValueLinkObject
                    body.Add(symbol);
                    break;
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
                    this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(syntaxTree.FilePath), "Generated");
                }
            }
        }

        private bool generatorOptionIsSet;
        private INamedTypeSymbol? valueLinkObjectAttributeSymbol;
        private INamedTypeSymbol? valueLinkGeneratorOptionAttributeSymbol;
    }
}
