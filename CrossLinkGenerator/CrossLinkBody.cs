// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
                this.GenerateHeader(ssb);
                var ns = ssb.ScopeNamespace(x.Key);

                rootObjects.AddRange(x.Value);

                var firstFlag = true;
                foreach (var y in x.Value)
                {
                    if (!firstFlag)
                    {
                        ssb.AppendLine();
                    }

                    firstFlag = false;

                    y.Generate(ssb, info);
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

            this.GenerateLoader(generator, info, rootObjects);
            this.FlushDiagnostic();
        }

        private void GenerateHeader(ScopingStringBuilder ssb)
        {
            ssb.AddHeader("// <auto-generated/>");
            ssb.AddUsing("System");
            ssb.AddUsing("System.Collections.Generic");
            ssb.AddUsing("System.Diagnostics.CodeAnalysis");
            ssb.AddUsing("System.Runtime.CompilerServices");
            ssb.AddUsing("Arc.Collection");
            ssb.AddUsing("CrossLink");
            ssb.AppendLine("#nullable enable", false);
            ssb.AppendLine("#pragma warning disable CS1591", false);
            // ssb.AppendLine("#pragma warning disable SA1306", false); // Field names should begin with lower-case letter
            // ssb.AppendLine("#pragma warning disable SA1401", false); // Fields should be private
            ssb.AppendLine();
        }

        private void GenerateLoader(CrossLinkGenerator generator, GeneratorInformation info, List<CrossLinkObject> rootObjects)
        {
            /*var ssb = new ScopingStringBuilder();
            this.GenerateHeader(ssb);

            using (var scopeFormatter = ssb.ScopeNamespace("Tinyhand.Formatters"))
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
            }*/
        }

        private void GenerateInitializer(CrossLinkGenerator generator, ScopingStringBuilder ssb, GeneratorInformation info)
        {
            // Namespace
            /*var ns = "CrossLink";
            if (!string.IsNullOrEmpty(generator.CustomNamespace))
            {// Custom namespace.
                ns = generator.CustomNamespace;
            }
            else if (!string.IsNullOrEmpty(generator.AssemblyName) &&
                generator.OutputKind != OutputKind.ConsoleApplication &&
                generator.OutputKind != OutputKind.WindowsApplication)
            {// To avoid namespace conflicts, use assembly name for namespace.
                ns = generator.AssemblyName;
            }

            info.ModuleInitializerClass.Add("Tinyhand.Formatters.Generated");

            ssb.AppendLine();
            using (var scopeCrossLink = ssb.ScopeNamespace(ns!))
            using (var scopeClass = ssb.ScopeBrace("public static class CrossLinkModule"))
            {
                ssb.AppendLine("private static bool Initialized;");
                ssb.AppendLine();
                if (info.UseModuleInitializer)
                {
                    ssb.AppendLine("[ModuleInitializer]");
                }

                using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
                {
                    ssb.AppendLine("if (CrossLinkModule.Initialized) return;");
                    ssb.AppendLine("CrossLinkModule.Initialized = true;");
                    ssb.AppendLine();

                    foreach (var x in info.ModuleInitializerClass)
                    {
                        ssb.Append(x, true);
                        ssb.AppendLine(".__gen__load();", false);
                    }
                }
            }*/
        }
    }
}
