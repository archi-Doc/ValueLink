// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace ValueLink.Generator
{
    public class Linkage
    {
        public static Linkage? Create(ValueLinkObject obj, VisceralAttribute attribute)
        {// obj: Member(field/property) or Class constructor
            LinkAttributeMock linkAttribute;
            try
            {
                linkAttribute = LinkAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                obj.Body.AddDiagnostic(ValueLinkBody.Error_AttributePropertyError, attribute.Location);
                return null;
            }

            var linkage = new Linkage();
            linkage.Location = attribute.Location;
            linkage.Type = linkAttribute.Type;
            linkage.Primary = linkAttribute.Primary;
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

            if (linkAttribute.TargetMember != string.Empty)
            {// Check target member
                var parent = obj.ContainingObject;
                if (parent == null)
                {
                    return null;
                }

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
                        if (target.Property_IsPrivateGetter || target.Property_IsPrivateSetter)
                        {
                            inaccessible = true;
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
                obj = target!;
            }

            linkage.AutoNotify = linkAttribute.AutoNotify;
            if (linkage.IsValidLink || linkage.AutoNotify)
            {// Valid link type or AutoNotify
                linkage.AutoLink = linkAttribute.AutoLink;

                if (linkAttribute.Name == string.Empty)
                {
                    if (!obj.Kind.IsValue())
                    {// Link name required (Constructor && No target member)
                        obj.Body.AddDiagnostic(ValueLinkBody.Error_LinkNameRequired, attribute.Location);
                        return null;
                    }
                    else
                    {
                        linkAttribute.Name = obj.SimpleName;

                        // Obsolete (id -> Id)
                        /*var name = obj.SimpleName.ToCharArray();
                        if (!char.IsLower(name[0]))
                        {// Link name must start with a lowercase letter.
                            obj.Body.AddDiagnostic(ValueLinkBody.Error_LinkTargetNameError, obj.Location, obj.SimpleName);
                        }
                        else
                        {
                            name[0] = char.ToUpperInvariant(name[0]);
                            linkAttribute.Name = new string(name);
                        }*/
                    }
                }

                linkage.Name = linkAttribute.Name + "Value";
                if (linkage.IsValidLink)
                {
                    linkage.LinkName = linkAttribute.Name + "Link";
                    linkage.ChainName = linkAttribute.Name + "Chain";
                }
            }

            return linkage;
        }

        public Location Location { get; private set; } = Location.None;

        public ChainType Type { get; private set; }

        public bool Primary { get; private set; }

        public bool AutoLink { get; private set; }

        public bool AutoNotify { get; private set; }

        public ValueLinkObject? Target { get; private set; } // Must be non-null if ChainType is ChainType.Ordered.

        public string TargetName { get; private set; } = string.Empty; // int id;

        public string Name { get; private set; } = string.Empty; // int Id { get; set; }

        public string LinkName { get; private set; } = string.Empty; // ListChain<int>.Link IdLink;

        public string ChainName { get; private set; } = string.Empty; // ListChain<int> IdChain

        public byte[] ChainNameUtf8 { get; set; } = Array.Empty<byte>();

        public string ChainNameIdentifier { get; set; } = string.Empty;

        public bool IsValidLink => this.Type != ChainType.None;

        public bool RequiresTarget => this.Type == ChainType.Ordered || this.Type == ChainType.ReverseOrdered || this.Type == ChainType.Unordered;
    }
}
