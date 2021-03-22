﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral
{
    public class VisceralIdentifier
    {
        public static VisceralIdentifier Default = new VisceralIdentifier();

        private HashSet<string> identifier = new();
        private string identifierFormat = "__gen_visceral_identifier__";
        private int identifierSerial = 0;

        public VisceralIdentifier()
        {
        }

        public bool Add(string identifier) => this.identifier.Add(identifier);

        public string GetIdentifier()
        {
            string identifier;
            while (true)
            {
                if (this.identifierSerial++ < 10_000)
                {
                    identifier = this.identifierFormat + this.identifierSerial.ToString("D4");
                }
                else
                {
                    identifier = this.identifierFormat + this.identifierSerial.ToString();
                }

                if (this.identifier.Add(identifier))
                {
                    return identifier;
                }
            }
        }
    }
}