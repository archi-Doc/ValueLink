// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValueLink.Generator;

/// <summary>Resolves closed owner types and emits formatter registration without runtime reflection.</summary>
internal sealed class StaticOwnerRegistration
{
    private readonly Compilation compilation;
    private readonly SourceProductionContext context;
    private readonly HashSet<ITypeSymbol> types = new(SymbolEqualityComparer.Default);
    private readonly Queue<ITypeSymbol> pendingTypes = new();
    private readonly HashSet<IMethodSymbol> methods = new(SymbolEqualityComparer.Default);
    private readonly Queue<IMethodSymbol> pendingMethods = new();
    private readonly Dictionary<IMethodSymbol, (ITypeSymbol?[] Types, IMethodSymbol[] Methods)> methodBodies = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<INamedTypeSymbol, string> owners = new(SymbolEqualityComparer.Default);
    private bool limitReported;

    internal StaticOwnerRegistration(Compilation compilation, SourceProductionContext context)
    {
        this.compilation = compilation;
        this.context = context;
    }

    internal bool HasErrors { get; private set; }

    internal (string Calls, string Bridges) Generate()
    {
        if (this.compilation.GetTypeByMetadataName("Tinyhand.TinyhandObjectAttribute") is null)
        {
            return (string.Empty, string.Empty);
        }

        foreach (var tree in this.compilation.SyntaxTrees)
        {
            this.context.CancellationToken.ThrowIfCancellationRequested();
            var model = this.compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot(this.context.CancellationToken).DescendantNodes())
            {
                if (node is TypeDeclarationSyntax declaration)
                {
                    this.AddType(model.GetDeclaredSymbol(declaration, this.context.CancellationToken));
                }
                else if (node is TypeSyntax type)
                {
                    this.AddType(model.GetTypeInfo(type, this.context.CancellationToken).Type);
                }
                else if (node is InvocationExpressionSyntax invocation)
                {
                    this.AddMethod(model.GetSymbolInfo(invocation, this.context.CancellationToken).Symbol as IMethodSymbol);
                }
            }
        }

        // Tinyhand's explicit roots also cover models used only in external generic helpers.
        foreach (var attribute in this.compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "Tinyhand.TinyhandRegisterAttribute")
            {
                foreach (var argument in attribute.ConstructorArguments)
                {
                    if (argument.Value is ITypeSymbol type)
                    {
                        this.AddType(type);
                    }
                }
            }
        }

        while (!this.HasErrors && (this.pendingTypes.Count > 0 || this.pendingMethods.Count > 0))
        {
            this.context.CancellationToken.ThrowIfCancellationRequested();
            if (this.pendingMethods.Count > 0)
            {
                this.ProcessMethod(this.pendingMethods.Dequeue());
            }
            else
            {
                this.ProcessType(this.pendingTypes.Dequeue());
            }
        }

        if (this.HasErrors)
        {
            return (string.Empty, string.Empty);
        }

        var calls = new StringBuilder();
        var bridges = new StringBuilder();
        var index = 0;
        foreach (var owner in this.owners.OrderBy(x => Name(x.Key), StringComparer.Ordinal))
        {
            var code = $"global::Tinyhand.Resolvers.GeneratedResolver.RegisterObject<{Name(owner.Key)}.@{owner.Value}>();";
            if (!this.compilation.IsSymbolAccessibleWithin(owner.Key, this.compilation.Assembly))
            {
                var scope = this.FindScope(owner.Key);
                if (scope is null)
                {
                    this.HasErrors = true;
                    this.context.ReportDiagnostic(Diagnostic.Create(ValueLinkBody.Error_InaccessibleOwnerRegistration, owner.Key.Locations.FirstOrDefault(), Name(owner.Key)));
                    continue;
                }

                code = this.Bridge(scope, code, "__ValueLinkRegisterOwner" + index++, bridges);
            }

            calls.AppendLine(code);
        }

        return (calls.ToString(), bridges.ToString());
    }

    private static string Name(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private bool IsClosed(ITypeSymbol type)
    {
        var remaining = 4096;
        return Check(type, 64);

        bool Check(ITypeSymbol candidate, int depth)
        {
            if (depth == 0 || --remaining < 0)
            {
                this.ReportLimit(type);
                return false;
            }

            return candidate switch
            {
                ITypeParameterSymbol => false,
                IArrayTypeSymbol array => Check(array.ElementType, depth - 1),
                INamedTypeSymbol named => named.TypeKind != TypeKind.Error && !named.IsAnonymousType && !named.IsUnboundGenericType &&
                    (named.ContainingType is null || Check(named.ContainingType, depth - 1)) && named.TypeArguments.All(x => Check(x, depth - 1)),
                _ => false,
            };
        }
    }

    private void ReportLimit(ITypeSymbol type)
    {
        if (!this.limitReported)
        {
            this.limitReported = true;
            this.HasErrors = true;
            this.context.ReportDiagnostic(Diagnostic.Create(ValueLinkBody.Error_UnboundedOwnerRegistration, type.Locations.FirstOrDefault(), type.Name));
        }
    }

    private void AddType(ITypeSymbol? type)
    {
        if (this.HasErrors || type is null || type.SpecialType == SpecialType.System_Void || type.IsRefLikeType || !this.IsClosed(type))
        {
            return;
        }

        type = type.WithNullableAnnotation(NullableAnnotation.None);
        if (this.types.Add(type))
        {
            if (this.types.Count > 16384)
            {
                this.ReportLimit(type);
                return;
            }

            this.pendingTypes.Enqueue(type);
        }
    }

    private void ProcessType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            this.AddType(array.ElementType);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        this.AddType(named.ContainingType);
        foreach (var argument in named.TypeArguments)
        {
            this.AddType(argument);
        }

        var attributes = named.GetAttributes();
        var link = attributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == ValueLinkObjectAttributeMock.FullName);
        var serializable = attributes.Any(x => x.AttributeClass?.ToDisplayString() is "Tinyhand.TinyhandObjectAttribute" or "Tinyhand.TinyhandUnionAttribute");
        if (link is not null && serializable)
        {
            var name = link.NamedArguments.FirstOrDefault(x => x.Key == "GoshujinClass").Value.Value as string;
            this.owners[named] = string.IsNullOrEmpty(name) ? "GoshujinClass" : name!;
        }

        // Follow concrete member types of source models and referenced Tinyhand models.
        if (SymbolEqualityComparer.Default.Equals(named.ContainingAssembly, this.compilation.Assembly) ||
            serializable)
        {
            this.AddType(named.BaseType);
            foreach (var member in named.GetMembers())
            {
                if (!member.IsImplicitlyDeclared && !member.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Tinyhand.IgnoreMemberAttribute"))
                {
                    this.AddType(member switch { IFieldSymbol field => field.Type, IPropertySymbol property => property.Type, _ => null });
                }
            }
        }
    }

    private void AddMethod(IMethodSymbol? method)
    {
        if (this.HasErrors || method is null || !this.IsClosed(method.ContainingType) || !method.TypeArguments.All(this.IsClosed) || !this.methods.Add(method))
        {
            return;
        }

        if (this.methods.Count > 16384)
        {
            this.ReportLimit(method.ContainingType);
            return;
        }

        this.pendingMethods.Enqueue(method);
    }

    private void ProcessMethod(IMethodSymbol method)
    {
        // A factory can expose a closed owner only through its return type, even when
        // its body is in another assembly and the caller uses an inferred type.
        this.AddType(method.ReturnType);
        foreach (var argument in method.TypeArguments)
        {
            this.AddType(argument);
        }

        if ((!method.IsGenericMethod && !method.ContainingType.IsGenericType) || method.DeclaringSyntaxReferences.Length == 0)
        {
            return;
        }

        var substitutions = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        for (var containing = method.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            for (var i = 0; i < containing.TypeArguments.Length; i++)
            {
                substitutions[containing.OriginalDefinition.TypeParameters[i]] = containing.TypeArguments[i];
            }
        }

        for (var i = 0; i < method.TypeArguments.Length; i++)
        {
            substitutions[method.OriginalDefinition.TypeParameters[i]] = method.TypeArguments[i];
        }

        var body = this.GetMethodBody(method);
        foreach (var type in body.Types)
        {
            this.AddType(this.Substitute(type, substitutions));
        }

        foreach (var invoked in body.Methods)
        {
            var containing = (INamedTypeSymbol)this.Substitute(invoked.ContainingType, substitutions)!;
            var definition = containing.GetMembers(invoked.Name).OfType<IMethodSymbol>()
                .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, invoked.OriginalDefinition)) ?? invoked.ConstructedFrom;
            this.AddMethod(invoked.IsGenericMethod
                ? definition.Construct(invoked.TypeArguments.Select(x => this.Substitute(x, substitutions)!).ToArray())
                : definition);
        }
    }

    private (ITypeSymbol?[] Types, IMethodSymbol[] Methods) GetMethodBody(IMethodSymbol method)
    {
        var definition = method.OriginalDefinition;
        if (this.methodBodies.TryGetValue(definition, out var body))
        {
            return body;
        }

        // Bind a generic helper once; only type substitution varies between closed calls.
        var types = new List<ITypeSymbol?>();
        var methods = new List<IMethodSymbol>();
        foreach (var reference in definition.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(this.context.CancellationToken);
            var model = this.compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var node in syntax.DescendantNodes())
            {
                if (node is TypeSyntax type)
                {
                    types.Add(model.GetTypeInfo(type, this.context.CancellationToken).Type);
                }
                else if (node is InvocationExpressionSyntax invocation &&
                    model.GetSymbolInfo(invocation, this.context.CancellationToken).Symbol is IMethodSymbol invoked)
                {
                    methods.Add(invoked);
                }
            }
        }

        body = (types.ToArray(), methods.ToArray());
        this.methodBodies.Add(definition, body);
        return body;
    }

    private ITypeSymbol? Substitute(ITypeSymbol? type, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        if (type is ITypeParameterSymbol parameter)
        {
            return substitutions.TryGetValue(parameter, out var argument) ? argument : type;
        }

        if (type is IArrayTypeSymbol array)
        {
            return this.compilation.CreateArrayTypeSymbol(this.Substitute(array.ElementType, substitutions)!, array.Rank);
        }

        if (type is INamedTypeSymbol named && named.TypeKind != TypeKind.Error && named.IsGenericType && !named.IsUnboundGenericType)
        {
            var definition = named.OriginalDefinition;
            if (named.ContainingType is { } containing)
            {
                var parent = (INamedTypeSymbol)this.Substitute(containing, substitutions)!;
                definition = parent.GetTypeMembers(named.Name, named.Arity).First();
            }

            return named.Arity == 0 ? definition : definition.Construct(named.TypeArguments.Select(x => this.Substitute(x, substitutions)!).ToArray());
        }

        return type;
    }

    private INamedTypeSymbol? FindScope(ITypeSymbol type)
    {
        return Candidates(type).FirstOrDefault(x => this.compilation.IsSymbolAccessibleWithin(type, x) &&
            SymbolEqualityComparer.Default.Equals(x.ContainingAssembly, this.compilation.Assembly) && CanGenerateBridge(x));

        static IEnumerable<INamedTypeSymbol> Candidates(ITypeSymbol target)
        {
            if (target is IArrayTypeSymbol array)
            {
                return Candidates(array.ElementType);
            }

            if (target is not INamedTypeSymbol named)
            {
                return Enumerable.Empty<INamedTypeSymbol>();
            }

            return (named.ContainingType is { } parent ? new[] { parent }.Concat(Candidates(parent)) : Enumerable.Empty<INamedTypeSymbol>())
                .Concat(named.TypeArguments.SelectMany(Candidates));
        }

        static bool CanGenerateBridge(INamedTypeSymbol scope)
        {
            for (var current = scope; current is not null; current = current.ContainingType)
            {
                if (current.DeclaringSyntaxReferences.Length == 0 || !current.DeclaringSyntaxReferences.All(r =>
                    r.GetSyntax() is TypeDeclarationSyntax d && d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private string Bridge(INamedTypeSymbol scope, string code, string methodName, StringBuilder output)
    {
        var ancestors = new Stack<INamedTypeSymbol>();
        for (var type = scope; type is not null; type = type.ContainingType)
        {
            ancestors.Push(type);
        }

        var ns = scope.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            output.Append("namespace ").Append(ns.ToDisplayString()).AppendLine(" {");
        }

        foreach (var type in ancestors)
        {
            var kind = type.IsRecord ? (type.IsValueType ? "record struct" : "record class") : type.TypeKind switch { TypeKind.Struct => "struct", TypeKind.Interface => "interface", _ => "class" };
            output.Append("partial ").Append(kind).Append(" @").Append(type.Name);
            if (type.Arity > 0)
            {
                output.Append('<').Append(string.Join(", ", type.TypeParameters.Select(x => "@" + x.Name))).Append('>');
            }

            output.AppendLine(" {");
        }

        output.Append("internal static void ").Append(methodName).AppendLine("() {").AppendLine(code).AppendLine("}");
        for (var i = 0; i < ancestors.Count; i++)
        {
            output.AppendLine("}");
        }

        if (!ns.IsGlobalNamespace)
        {
            output.AppendLine("}");
        }

        var call = $"{Name(scope)}.{methodName}();";
        return this.compilation.IsSymbolAccessibleWithin(scope, this.compilation.Assembly) ? call : this.Bridge(scope.ContainingType!, call, methodName, output);
    }
}
