﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValueLink.Generator;

/* [Generator]
public class ValueLinkGenerator : ISourceGenerator, IGeneratorInformation
{
    public bool AttachDebugger { get; private set; } = false;

    public bool GenerateToFile { get; private set; } = false;

    public string? CustomNamespace { get; private set; }

    public bool ModuleInitializerIsAvailable { get; private set; } = false;

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public GeneratorExecutionContext Context { get; private set; }

    private ValueLinkBody body = default!;
    private INamedTypeSymbol? valueLinkObjectAttributeSymbol;
    private INamedTypeSymbol? linkAttributeSymbol;
    private INamedTypeSymbol? valueLinkGeneratorOptionAttributeSymbol;
#pragma warning disable RS1024
    private HashSet<INamedTypeSymbol?> processedSymbol = new();
#pragma warning restore RS1024

    static ValueLinkGenerator()
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            this.Context = context;

            if (!(context.SyntaxReceiver is ValueLinkSyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;

            this.valueLinkObjectAttributeSymbol = compilation.GetTypeByMetadataName(ValueLinkObjectAttributeMock.FullName);
            if (this.valueLinkObjectAttributeSymbol == null)
            {
                return;
            }

            this.linkAttributeSymbol = compilation.GetTypeByMetadataName(LinkAttributeMock.FullName);
            if (this.linkAttributeSymbol == null)
            {
                return;
            }

            this.valueLinkGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(ValueLinkGeneratorOptionAttributeMock.FullName);
            if (this.valueLinkGeneratorOptionAttributeSymbol == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            this.ProcessGeneratorOption(receiver, compilation);
            if (this.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            this.Prepare(context, compilation);

            context.CancellationToken.ThrowIfCancellationRequested();
            this.body = new ValueLinkBody(context);
            receiver.Generics.Prepare(compilation);

            // IN: type declaration
            context.CancellationToken.ThrowIfCancellationRequested();
            foreach (var x in receiver.CandidateSet)
            {
                var model = compilation.GetSemanticModel(x.SyntaxTree);
                if (model.GetDeclaredSymbol(x) is INamedTypeSymbol s)
                {
                    this.ProcessSymbol(s);
                }
            }

            // IN: close generic (member, expression)
            context.CancellationToken.ThrowIfCancellationRequested();
            foreach (var ts in receiver.Generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric).Select(a => a.TypeSymbol))
            {
                if (ts != null)
                {
                    this.ProcessSymbol(ts);
                }
            }

            this.SalvageCloseGeneric(receiver.Generics);

            context.CancellationToken.ThrowIfCancellationRequested();
            this.body.Prepare();
            if (this.body.Abort)
            {
                return;
            }

            this.body.Generate(this, context.CancellationToken);
        }
        catch
        {
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        // System.Diagnostics.Debugger.Launch();

        context.RegisterForSyntaxNotifications(() => new ValueLinkSyntaxReceiver());
    }

    private void SalvageCloseGeneric(VisceralGenerics generics)
    {
        var stack = new Stack<INamedTypeSymbol>();
        foreach (var x in generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric))
        {
            SalvageCloseGenericCore(stack, x.TypeSymbol);
        }

        void SalvageCloseGenericCore(Stack<INamedTypeSymbol> stack, INamedTypeSymbol? ts)
        {
            if (ts == null || stack.Contains(ts))
            {// null or already exists.
                return;
            }
            else if (ts.TypeKind != TypeKind.Class && ts.TypeKind != TypeKind.Struct)
            {// Not type
                return;
            }
            else if (VisceralHelper.TypeToGenericsKind(ts) != VisceralGenericsKind.ClosedGeneric)
            {// Not close generic
                return;
            }

            this.ProcessSymbol(ts);

            stack.Push(ts);
            try
            {
                foreach (var y in ts.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()))
                {
                    INamedTypeSymbol? nts = null;
                    if (y is IFieldSymbol fs)
                    {
                        nts = fs.Type as INamedTypeSymbol;
                    }
                    else if (y is IPropertySymbol ps)
                    {
                        nts = ps.Type as INamedTypeSymbol;
                    }

                    // not primitive
                    if (nts != null && nts.SpecialType == SpecialType.None)
                    {
                        SalvageCloseGenericCore(stack, nts);
                    }
                }
            }
            finally
            {
                stack.Pop();
            }
        }
    }

    private void ProcessSymbol(INamedTypeSymbol s)
    {
        if (this.processedSymbol.Contains(s))
        {
            return;
        }

        this.processedSymbol.Add(s);
        foreach (var x in s.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.valueLinkObjectAttributeSymbol))
            { // ValueLinkObject
                var obj = this.body.Add(s);
                break;
            }
        }
    }

    private void Prepare(GeneratorExecutionContext context, Compilation compilation)
    {
        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;

        if (context.ParseOptions.PreprocessorSymbolNames.Any(x => x == "NET5_0"))
        {// .NET 5
            this.ModuleInitializerIsAvailable = true;
        }
        else
        {
            if (compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is { } atr2 && atr2.DeclaredAccessibility == Accessibility.Public)
            {// [ModuleInitializer] is supported.
                this.ModuleInitializerIsAvailable = true;
            }
        }
    }

    private void ProcessGeneratorOption(ValueLinkSyntaxReceiver receiver, Compilation compilation)
    {
        if (receiver.GeneratorOptionSyntax == null)
        {
            return;
        }

        var model = compilation.GetSemanticModel(receiver.GeneratorOptionSyntax.SyntaxTree);
        if (model.GetDeclaredSymbol(receiver.GeneratorOptionSyntax) is INamedTypeSymbol s)
        {
            var attr = s.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.valueLinkGeneratorOptionAttributeSymbol));
            if (attr != null)
            {
                var va = new VisceralAttribute(ValueLinkGeneratorOptionAttributeMock.FullName, attr);
                var ta = ValueLinkGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                this.AttachDebugger = ta.AttachDebugger;
                this.GenerateToFile = ta.GenerateToFile;
                this.CustomNamespace = ta.CustomNamespace;
                this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(receiver.GeneratorOptionSyntax.SyntaxTree.FilePath), "Generated");
            }
        }
    }

    internal class ValueLinkSyntaxReceiver : ISyntaxReceiver
    {
        public TypeDeclarationSyntax? GeneratorOptionSyntax { get; private set; }

        public HashSet<TypeDeclarationSyntax> CandidateSet { get; } = new HashSet<TypeDeclarationSyntax>();

        public VisceralGenerics Generics { get; } = new VisceralGenerics();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax typeSyntax)
            {// Our target is a type syntax.
                if (this.CheckAttribute(typeSyntax))
                {// If the type has the specific attribute.
                    this.CandidateSet.Add(typeSyntax);
                }
            }
            else if (syntaxNode is GenericNameSyntax genericSyntax)
            {// Generics
                this.Generics.Add(genericSyntax);
            }
        }

        /// <summary>
        /// Returns true if the Type Sytax contains the specific attribute.
        /// </summary>
        /// <param name="typeSyntax">A type syntax.</param>
        /// <returns>True if the Type Sytax contains the specific attribute.</returns>
        private bool CheckAttribute(TypeDeclarationSyntax typeSyntax)
        {
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (this.GeneratorOptionSyntax == null)
                    {
                        if (name.EndsWith(ValueLinkGeneratorOptionAttributeMock.StandardName) || name.EndsWith(ValueLinkGeneratorOptionAttributeMock.SimpleName))
                        {
                            this.GeneratorOptionSyntax = typeSyntax;
                        }
                    }

                    if (name.EndsWith(ValueLinkObjectAttributeMock.StandardName) ||
                        name.EndsWith(ValueLinkObjectAttributeMock.SimpleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}*/
