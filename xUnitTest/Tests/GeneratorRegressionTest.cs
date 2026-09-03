// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using Xunit;
using LinkedObject = ValueLink.ValueLinkObjectAttribute;

namespace xUnitTest;

/// <summary>
/// Provides a linked fixture whose ValueLink attribute is referenced through an alias.
/// </summary>
[LinkedObject]
public partial class AliasedValueLinkItem : IEquatableObject
{
    [Link(Type = ChainType.List, Name = "Items", Primary = true)]
    public int Id { get; set; }

    bool IEquatableObject.ObjectEquals(object? other) => other is AliasedValueLinkItem item && this.Id == item.Id;
}

/// <summary>
/// Tests aliased attributes and generated equality.
/// </summary>
public class GeneratorRegressionTest
{
    [Fact]
    public void AliasedAttributeGeneratesUsableOwnerAndEquality()
    {
        var first = new AliasedValueLinkItem.GoshujinClass();
        var second = new AliasedValueLinkItem.GoshujinClass();
        Assert.True(first.ObjectEquals(second));
        first.Add(new() { Id = 1 });
        Assert.False(first.ObjectEquals(second));
        second.Add(new() { Id = 1 });
        Assert.True(first.ObjectEquals(second));
        second.ItemsChain[0].Id = 2;
        Assert.False(first.ObjectEquals(second));
        Assert.False(first.ObjectEquals(null));
    }
}
