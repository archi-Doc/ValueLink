﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace CrossLink.Generator
{
    public class CrossLinkBody : VisceralBody<CrossLinkObject>
    {
        public static readonly string DefaultGoshujinClass = "GoshujinClass";
        public static readonly string DefaultGoshujinInstance = "Goshujin";
        public static readonly string ExplicitPropertyChanged = "PropertyChanged";

        public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
            id: "CLG001", title: "Not a partial class/struct", messageFormat: "CrossLinkObject '{0}' is not a partial class/struct",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
            id: "CLG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
            id: "CLG003", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_KeywordUsed = new DiagnosticDescriptor(
            id: "CLG004", title: "Keyword used", messageFormat: "The type '{0}' already contains a definition for '{1}'",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_LinkTargetNotField = new DiagnosticDescriptor(
            id: "CLG005", title: "Not field", messageFormat: "The target of the link '{0}' must be a field",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_LinkTargetNameError = new DiagnosticDescriptor(
            id: "CLG006", title: "Name error", messageFormat: "The field '{0}' to be linked must start with a lowercase letter",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_MultipleLink = new DiagnosticDescriptor(
            id: "CLG007", title: "Link error", messageFormat: "One link is allowed per member, consider adding a LinkAttribute to a constructor",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NoLinkTarget = new DiagnosticDescriptor(
            id: "CLG008", title: "Link error", messageFormat: "This type of link requires a property or field to be linked",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_NoNotifyTarget = new DiagnosticDescriptor(
            id: "CLG009", title: "Link error", messageFormat: "AutoNotify option requires a property or field to be linked",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_LinkNameRequired = new DiagnosticDescriptor(
            id: "CLG010", title: "Link error", messageFormat: "Link name is required",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_PropertyChanged = new DiagnosticDescriptor(
            id: "CLG011", title: "INotifyPropertyChanged implementation", messageFormat: "{0} must implement INotifyPropertyChanged directly (PropertyChanged.Invoke() is not allowed to be called from inherited classes)",
            category: "CrossLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_StringKeySizeLimit = new DiagnosticDescriptor(
            id: "CLG012", title: "String key limit", messageFormat: "The size of the string key exceeds the limit {0}",
            category: "CrossLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_StringKeyConflict = new DiagnosticDescriptor(
            id: "CLG013", title: "String keys conflict", messageFormat: "String keys with the same name were detected",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Error_StringKeyNull = new DiagnosticDescriptor(
            id: "CLG014", title: "String key null", messageFormat: "String key cannot contain null character",
            category: "CrossLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NoPrimaryLink = new DiagnosticDescriptor(
            id: "CLG015", title: "No primary link", messageFormat: "Consider specifying a primary link that holds all objects in the collection in order to maximize the performance of serialization",
            category: "CrossLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor Warning_NoKeyAttribute = new DiagnosticDescriptor(
            id: "CLG016", title: "No Key attribute", messageFormat: "Consider adding Key or KeyAsName attribute to this member so that CrossLink can serialize it properly",
            category: "CrossLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public CrossLinkBody(GeneratorExecutionContext context)
            : base(context)
        {
        }

        internal Dictionary<string, List<CrossLinkObject>> Namespaces = new();

        public void Prepare()
        {
            // Configure objects.
            var array = this.FullNameToObject.ToArray();
            foreach (var x in array)
            {
                x.Value.Configure();
            }

            this.FlushDiagnostic();
            if (this.Abort)
            {
                return;
            }

            array = this.FullNameToObject.Where(x => x.Value.ObjectAttribute != null).ToArray();
            foreach (var x in array)
            {
                x.Value.ConfigureRelation();
            }

            // Check
            foreach (var x in array)
            {
                x.Value.Check();
            }

            this.FlushDiagnostic();
            if (this.Abort)
            {
                return;
            }
        }

        public void Generate(CrossLinkGenerator generator)
        {
            ScopingStringBuilder ssb = new();
            GeneratorInformation info = new()
            {
                UseModuleInitializer = generator.ModuleInitializerIsAvailable,
            };
            List<CrossLinkObject> rootObjects = new();

            // Namespace
            foreach (var x in this.Namespaces)
            {
                var tinyhandFlag = x.Value.Any(a => a.ObjectFlag.HasFlag(CrossLinkObjectFlag.TinyhandObject)); // has TinyhandObjectAttribute
                this.GenerateHeader(ssb, tinyhandFlag);
                var ns = ssb.ScopeNamespace(x.Key);

                rootObjects.AddRange(x.Value); // For loader generation

                var firstFlag = true;
                foreach (var y in x.Value)
                {
                    if (!firstFlag)
                    {
                        ssb.AppendLine();
                    }

                    firstFlag = false;

                    y.Generate(ssb, info); // Primary TinyhandObject
                }

                var result = ssb.Finalize();

                if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
                {
                    this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.CrossLink.{x.Key}.cs"));
                }
                else
                {
                    this.Context?.AddSource($"gen.CrossLink.{x.Key}", SourceText.From(result, Encoding.UTF8));
                }
            }

            if (info.UseTinyhand)
            {
                this.GenerateLoader(generator, info, rootObjects);
            }

            this.FlushDiagnostic();
        }

        private void GenerateHeader(ScopingStringBuilder ssb, bool tinyhandFlag)
        {
            ssb.AddHeader("// <auto-generated/>");
            ssb.AddUsing("System");
            ssb.AddUsing("System.Collections.Generic");
            ssb.AddUsing("System.Diagnostics.CodeAnalysis");
            ssb.AddUsing("System.Runtime.CompilerServices");
            ssb.AddUsing("Arc.Collection");
            ssb.AddUsing("CrossLink");
            if (tinyhandFlag)
            {
                ssb.AddUsing("Tinyhand");
                ssb.AddUsing("Tinyhand.IO");
                ssb.AddUsing("Tinyhand.Resolvers");
            }

            ssb.AppendLine("#nullable enable", false);
            ssb.AppendLine("#pragma warning disable CS1591", false);
            // ssb.AppendLine("#pragma warning disable SA1306", false); // Field names should begin with lower-case letter
            // ssb.AppendLine("#pragma warning disable SA1401", false); // Fields should be private
            ssb.AppendLine();
        }

        private void GenerateLoader(CrossLinkGenerator generator, GeneratorInformation info, List<CrossLinkObject> rootObjects)
        {
            var ssb = new ScopingStringBuilder();
            this.GenerateHeader(ssb, true);

            using (var scopeFormatter = ssb.ScopeNamespace("CrossLink.Generator"))
            {
                using (var methods = ssb.ScopeBrace("static class Generated"))
                {
                    info.FinalizeBlock(ssb);

                    CrossLinkObject.GenerateLoader(ssb, info, rootObjects);
                }
            }

            this.GenerateInitializer(generator, ssb, info);

            var result = ssb.Finalize();

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.CrossLinkGenerated.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.CrossLinkLoader", SourceText.From(result, Encoding.UTF8));
            }
        }

        private void GenerateInitializer(CrossLinkGenerator generator, ScopingStringBuilder ssb, GeneratorInformation info)
        {
            // Namespace
            var ns = "CrossLink";
            var assemblyId = string.Empty; // Assembly ID
            if (!string.IsNullOrEmpty(generator.CustomNamespace))
            {// Custom namespace.
                ns = generator.CustomNamespace;
            }
            else
            {// Other (Apps)
                // assemblyId = "_" + generator.AssemblyId.ToString("x");
                if (!string.IsNullOrEmpty(generator.AssemblyName))
                {
                    assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
                }
            }

            info.ModuleInitializerClass.Add("CrossLink.Generator.Generated");

            ssb.AppendLine();
            using (var scopeCrossLink = ssb.ScopeNamespace(ns!))
            using (var scopeClass = ssb.ScopeBrace("public static class CrossLinkModule" + assemblyId))
            {
                ssb.AppendLine("private static bool Initialized;");
                ssb.AppendLine();
                if (info.UseModuleInitializer)
                {
                    ssb.AppendLine("[ModuleInitializer]");
                }

                using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
                {
                    ssb.AppendLine("if (Initialized) return;");
                    ssb.AppendLine("Initialized = true;");
                    ssb.AppendLine();

                    foreach (var x in info.ModuleInitializerClass)
                    {
                        ssb.Append(x, true);
                        ssb.AppendLine(".__gen__cl();", false);
                    }
                }
            }
        }
    }
}
