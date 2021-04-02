// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace CrossLink.Generator
{
    public class Linkage
    {
        public static Linkage? Create(CrossLinkObject obj, VisceralAttribute attribute)
        {
            LinkAttributeMock linkAttribute;
            try
            {
                linkAttribute = LinkAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                obj.Body.AddDiagnostic(CrossLinkBody.Error_AttributePropertyError, attribute.Location);
                return null;
            }

            var linkage = new Linkage();
            linkage.Location = attribute.Location;
            linkage.Type = linkAttribute.Type;
            linkage.Prime = linkAttribute.Prime;
            if (obj.Kind.IsValue())
            {
                linkage.Target = obj;
                linkage.TargetName = obj.SimpleName;
            }

            linkage.AutoNotify = linkAttribute.AutoNotify;
            if (linkage.IsValidLink || linkage.AutoNotify)
            {// Valid link type or AutoNotify
                linkage.AutoLink = linkAttribute.AutoLink;

                if (linkAttribute.Name == string.Empty)
                {
                    if (!obj.Kind.IsValue())
                    {// Link name required
                        obj.Body.AddDiagnostic(CrossLinkBody.Error_LinkNameRequired, attribute.Location);
                        return null;
                    }
                    else
                    {
                        var name = obj.SimpleName.ToCharArray();
                        if (!char.IsLower(name[0]))
                        {// Link name must start with a lowercase letter.
                            obj.Body.AddDiagnostic(CrossLinkBody.Error_LinkTargetNameError, obj.Location, obj.SimpleName);
                        }
                        else
                        {
                            name[0] = char.ToUpperInvariant(name[0]);
                            linkAttribute.Name = new string(name);
                        }
                    }
                }

                linkage.Name = linkAttribute.Name;
                if (linkage.IsValidLink)
                {
                    linkage.LinkName = linkage.Name + "Link";
                    linkage.ChainName = linkage.Name + "Chain";
                }
            }

            return linkage;
        }

        public Location Location { get; private set; } = Location.None;

        public LinkType Type { get; private set; }

        public bool Prime { get; private set; }

        public bool AutoLink { get; private set; }

        public bool AutoNotify { get; private set; }

        public CrossLinkObject? Target { get; private set; } // Must be non-null if LinkType is LinkType.Ordered.

        public string TargetName { get; private set; } = string.Empty; // int id;

        public string Name { get; private set; } = string.Empty; // int Id { get; set; }

        public string LinkName { get; private set; } = string.Empty; // ListChain<int>.Link IdLink;

        public string ChainName { get; private set; } = string.Empty; // ListChain<int> IdChain

        public byte[] ChainNameUtf8 { get; set; } = Array.Empty<byte>();

        public string ChainNameIdentifier { get; set; } = string.Empty;

        public bool IsValidLink => this.Type != LinkType.None;

        public bool RequiresTarget => this.Type == LinkType.Ordered;
    }
}
