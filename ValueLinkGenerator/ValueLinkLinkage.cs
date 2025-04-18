﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace ValueLink.Generator;

public class Linkage
{
    public static Linkage? Create(ValueLinkObject obj, VisceralAttribute attribute)
    {// obj: Member(field/property) or Class constructor
        LinkAttributeMock linkAttribute;
        try
        {
            var restricted = obj.ContainingObject?.ObjectAttribute?.Restricted == true;
            linkAttribute = LinkAttributeMock.FromArray(restricted, attribute.ConstructorArguments, attribute.NamedArguments);
        }
        catch (InvalidCastException)
        {
            obj.Body.AddDiagnostic(ValueLinkBody.Error_AttributePropertyError, attribute.Location);
            return null;
        }

        var parent = obj.ContainingObject;
        if (parent is null || obj.TypeObject is null)
        {
            return null;
        }

        var linkage = new Linkage();
        linkage.Location = attribute.Location;
        linkage.LinkedObject = obj;
        linkage.Type = linkAttribute.Type;
        linkage.TypeObject = obj.TypeObject;
        linkage.Primary = linkAttribute.Primary;
        linkage.Unique = linkAttribute.Unique;

        if (!string.IsNullOrEmpty(linkAttribute.UnsafeTargetChain))
        {
            linkage.UnsafeTargetChain = true;
            linkage.Type = ChainType.None;
            linkage.Primary = false;
            linkage.Unique = false;
        }

        if (obj.Kind.IsValue())
        {
            linkage.Target = obj;
            linkage.TargetName = obj.SimpleName;

            if (linkAttribute.TargetMember != string.Empty)
            {// Target member is only supported for LinkAttribute annotated to the constructor.
                obj.Body.AddDiagnostic(ValueLinkBody.Error_LinkMember, attribute.Location);
                return null;
            }
        }

        if (linkage.Unique)
        {
            if (!linkage.Type.IsLocatable())
            {
                obj.Body.AddDiagnostic(ValueLinkBody.Error_UniqueLinkType, attribute.Location);
                return null;
            }
        }

        if (linkAttribute.TargetMember != string.Empty)
        {// Check target member
            var target = parent.GetMembers(VisceralTarget.FieldProperty).FirstOrDefault(x => x.SimpleName == linkAttribute.TargetMember);
            if (target == null)
            {// 'obj.FullName' does not contain a class member named 'linkAttribute.TargetMember'.
                obj.Body.AddDiagnostic(ValueLinkBody.Error_NoTargetMember, attribute.Location, parent.FullName, linkAttribute.TargetMember);
                return null;
            }

            var inaccessible = false;
            if (target.IsInitOnly)
            {
                inaccessible = true;
            }
            else if (target.ContainingObject != parent)
            {
                if (target.Kind == VisceralObjectKind.Field)
                {
                    if (target.Field_IsPrivate)
                    {
                        inaccessible = true;
                    }
                }
                else if (target.Kind == VisceralObjectKind.Property)
                {
                    if (target.Property_IsPrivateGetter)
                    {
                        inaccessible = true;
                    }
                    else if (target.Property_IsPrivateSetter)
                    {
                        linkAttribute.AddValue = false; // inaccessible = true;
                    }
                }
            }

            if (inaccessible)
            {
                obj.Body.AddDiagnostic(ValueLinkBody.Error_InaccessibleMember, attribute.Location, target.FullName);
                return null;
            }

            linkage.Target = target;
            linkage.TargetName = linkAttribute.TargetMember;
            if (target.TypeObject is not null)
            {
                linkage.TypeObject = target.TypeObject;
            }

            obj = target!;
        }

        linkage.AutoNotify = linkAttribute.AutoNotify;
        if (linkage.IsValidLink || linkage.AutoNotify || !string.IsNullOrEmpty(linkAttribute.UnsafeTargetChain))
        {// Valid link type or AutoNotify
            linkage.AutoLink = linkAttribute.AutoLink;
            if (linkAttribute.Type == ChainType.SlidingList)
            {
                linkage.AutoLink = false;
            }

            if (linkAttribute.Name == string.Empty)
            {
                if (!obj.Kind.IsValue())
                {// Link name required (Constructor && No target member)
                    obj.Body.AddDiagnostic(ValueLinkBody.Error_LinkNameRequired, attribute.Location);
                    return null;
                }
                else
                {
                    if (obj.SimpleName.Length > 0 && char.IsLower(obj.SimpleName[0]))
                    {// id -> Id
                        var name = obj.SimpleName.ToCharArray();
                        name[0] = char.ToUpperInvariant(name[0]);
                        linkAttribute.Name = new string(name);
                    }
                    else
                    {
                        linkAttribute.Name = obj.SimpleName;
                    }

                    // Obsolete
                    // Link name must start with a lowercase letter.
                    // obj.Body.AddDiagnostic(ValueLinkBody.Error_LinkTargetNameError, obj.Location, obj.SimpleName);
                }
            }

            linkage.ValueName = linkAttribute.Name + "Value";
            if (linkage.IsValidLink)
            {
                linkage.LinkName = linkAttribute.Name + "Link";
                linkage.ChainName = linkAttribute.Name + "Chain";
            }
            else if (!string.IsNullOrEmpty(linkAttribute.UnsafeTargetChain))
            {
                linkage.LinkName = linkAttribute.Name + "Link";
                linkage.ChainName = linkAttribute.UnsafeTargetChain;
            }
        }

        // Accessibility
        linkage.Accessibility = linkAttribute.Accessibility;
        if (linkage.IsValidLink)
        {
            if (obj.Kind == VisceralObjectKind.Property)
            {
                var property = obj.Property_Accessibility;
                linkage.GetterAccessibility = property.Getter;
                linkage.SetterAccessibility = property.Setter;
            }
            else if (obj.Kind == VisceralObjectKind.Field)
            {
                linkage.GetterAccessibility = obj.Field_Accessibility;
                linkage.SetterAccessibility = linkage.GetterAccessibility;
            }

            if (linkage.Accessibility == ValueLinkAccessibility.PublicGetter)
            {
                linkage.GetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Public;
            }
            else if (linkage.Accessibility == ValueLinkAccessibility.Public)
            {
                linkage.GetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Public;
                linkage.SetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Public;
            }
            else if (linkage.Accessibility == ValueLinkAccessibility.Protected)
            {
                linkage.GetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Protected;
                linkage.SetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Protected;
            }
            else if (linkage.Accessibility == ValueLinkAccessibility.Private)
            {
                linkage.GetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Private;
                linkage.SetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Private;
            }
        }

        // No value
        linkage.AddValue = linkAttribute.AddValue;
        if (linkage.AutoNotify && !linkage.AddValue)
        {
            linkage.AddValue = true;
            obj.Body.AddDiagnostic(ValueLinkBody.Warning_AutoNotifyEnabled, attribute.Location);
        }

        if (linkage.SetterAccessibility == Microsoft.CodeAnalysis.Accessibility.NotApplicable)
        {// Value property will not be added since the setter is unavailable.
            linkage.AddValue = false;
        }

        if (obj.IsPartialProperty)
        {
            linkage.AddValue = true;
            linkage.ValueName = linkAttribute.Name;
        }

        if (!string.IsNullOrEmpty(linkage.LinkName))
        {// Methods (Predicate, Adding, Removing)
            var predicateName = linkage.LinkName + ValueLinkBody.PredicateMethodName;
            var addedName = linkage.LinkName + ValueLinkBody.AddedMethodName;
            var removedName = linkage.LinkName + ValueLinkBody.RemovedMethodName;
            foreach (var x in parent.GetMembers(VisceralTarget.Method).Where(y => y.Method_Parameters.Length == 0))
            {
                if (x.SimpleName == predicateName)
                {
                    if (x.Method_ReturnObject?.FullName == "bool")
                    {// bool LinkNamePredicate()
                        linkage.PredicateMethodName = predicateName;
                    }
                }
                else if (x.Method_ReturnObject?.FullName == "void")
                {
                    if (x.SimpleName == addedName)
                    {// void LinkNameAdded()
                        linkage.AddedMethodName = addedName;
                    }
                    else if (x.SimpleName == removedName)
                    {// void LinkNameRemoved()
                        linkage.RemovedMethodName = removedName;
                    }
                }
            }
        }

        return linkage;
    }

    public Linkage? MainLink { get; set; }

    public Location Location { get; private set; } = Location.None;

    public ChainType Type { get; private set; }

    public ValueLinkObject LinkedObject { get; private set; } = default!;

    public ValueLinkObject TypeObject { get; private set; } = default!;

    public Member? Member { get; set; }

    public bool Primary { get; private set; }

    public bool Unique { get; private set; }

    public bool AutoLink { get; private set; }

    public bool AutoNotify { get; private set; }

    public ValueLinkObject? Target { get; private set; } // Must be non-null if ChainType is ChainType.Ordered.

    public string TargetName { get; private set; } = string.Empty; // int id;

    public ValueLinkAccessibility Accessibility { get; private set; }

    public string ValueName { get; set; } = string.Empty; // int Id { get; set; }

    public string LinkName { get; private set; } = string.Empty; // ListChain<int>.Link IdLink;

    public string ChainName { get; private set; } = string.Empty; // ListChain<int> IdChain

    public bool UnsafeTargetChain { get; private set; } = false;

    public bool IsValidLink
        => this.Type != ChainType.None;

    public bool IsSharedOrInvalid
        => this.UnsafeTargetChain || this.Type == ChainType.None;

    public bool RequiresTarget => this.Type == ChainType.Ordered || this.Type == ChainType.ReverseOrdered || this.Type == ChainType.Unordered;

    public Accessibility GetterAccessibility { get; private set; } = Microsoft.CodeAnalysis.Accessibility.Public;

    public Accessibility SetterAccessibility { get; private set; } = Microsoft.CodeAnalysis.Accessibility.Public;

    public bool AddValue { get; private set; }

    public string? PredicateMethodName { get; set; }

    public string? AddedMethodName { get; set; }

    public string? RemovedMethodName { get; set; }

    internal void SetChainType(ChainType chainType)
    {
        this.Type = chainType;
    }

    internal void SetRepeatableRead()
    {
        this.AddValue = false;
        this.GetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Private;
        this.SetterAccessibility = Microsoft.CodeAnalysis.Accessibility.Private;
    }
}
