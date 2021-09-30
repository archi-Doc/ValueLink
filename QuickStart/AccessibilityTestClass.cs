// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

namespace QuickStart;

[ValueLinkObject]
public partial class AccessibilityTestClass
{
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    private int privateField_PublicGetter;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    private int privateField_Public;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    private int privateField_Inherit;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    protected int protectedField_PublicGetter;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    protected int protectedField_Public;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    protected int protectedField_Inherit;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    public int publicField_PublicGetter;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public int publicField_Public;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    public int publicField_Inherit;

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    private int PrivateProperty_PublicGetter { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    private int PrivateProperty_Public { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    private int PrivateProperty_Inherit { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    protected int ProtectedProperty_PublicGetter { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    protected int ProtectedProperty_Public { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    protected int ProtectedProperty_Inherit { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    public int PublicProperty_PublicGetter { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public int PublicProperty_Public { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    public int PublicProperty_Inherit { get; set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.PublicGetter)]
    public int PublicPrivateProperty_PublicGetter { get; private set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    public int PublicPrivateProperty_Public { get; private set; }

    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Inherit)]
    public int PublicPrivateProperty_Inherit { get; private set; }
}
