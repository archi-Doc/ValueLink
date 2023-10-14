﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record PropertyAccessibilityClass : IEquatableObject<PropertyAccessibilityClass>
{
    [Key(0, AddProperty = "Id", PropertyAccessibility = PropertyAccessibility.PublicSetter)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private int _id;

    [Key(1, AddProperty = "B", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    private int _b;

    [Key(2, AddProperty = "C", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private int _c = 3;

    [Key(3, AddProperty = "X")]
    [MaxLength(10)]
    private string _x = string.Empty;

    [Key(4, AddProperty = "Y", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    [Link(Type = ChainType.Unordered)]
    private string _y = string.Empty;

    [Key(5, AddProperty = "Z", PropertyAccessibility = PropertyAccessibility.PublicSetter)]
    [Link(Type = ChainType.Ordered)]
    private string _z = string.Empty;

    bool IEquatableObject<PropertyAccessibilityClass>.ObjectEquals(PropertyAccessibilityClass other)
    {
        return this._id == other._id && this._b == other._b && this._c == other._c && this._x == other._x;
    }
}

public class PropertyAccessibilityTest
{
    [Fact]
    public void Test1()
    {
        var g = new PropertyAccessibilityClass.GoshujinClass();
        using (var w = g.TryLock(1)!)
        {
            w.B = 1;
            // w.C = 2;
            w.X = "One";
        }

        var r = g.TryGet(0);
        r.IsNull();
        r = g.TryGet(1);
        r.IsNotNull();

        var b = TinyhandSerializer.Serialize(g);
        var g2 = TinyhandSerializer.Deserialize<PropertyAccessibilityClass.GoshujinClass>(b);
        g.GoshujinEquals(g2!).IsTrue();
    }
}
