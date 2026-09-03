// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;

namespace NativeAotTest;

/// <summary>Exercises closed generic owner registration across assembly boundaries.</summary>
/// <typeparam name="T">The serialized payload type.</typeparam>
[TinyhandObject]
[ValueLinkObject]
public partial class GenericItem<T>
{
    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true)]
    public int Id { get; set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}
