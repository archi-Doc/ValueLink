// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#if !NET6_0_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace System.Runtime.CompilerServices;

/// <summary>
/// Marks a type as an interpolated string handler (polyfill for netstandard2.0).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class InterpolatedStringHandlerAttribute : Attribute
{
}

/// <summary>
/// Names the parameters passed to an interpolated string handler constructor (polyfill for netstandard2.0).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute(string argument) => this.Arguments = [argument];

    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => this.Arguments = arguments;

    public string[] Arguments { get; }
}
#endif
