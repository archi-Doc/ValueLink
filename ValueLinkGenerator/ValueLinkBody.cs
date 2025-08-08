// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace ValueLink.Generator;

public static class TinyhandBody
{
    public static readonly string IStructualRoot = "IStructualRoot";
    public static readonly string IStructualObject = "IStructualObject";
    public static readonly string ITinyhandCustomJournal = "Tinyhand.ITinyhandCustomJournal";
}

public class ValueLinkBody : VisceralBody<ValueLinkObject>
{
    public const int StackallocThreshold = 4096;
    public static readonly string DefaultGoshujinClass = "GoshujinClass";
    public static readonly string DefaultGoshujinInstance = "Goshujin";
    public static readonly string ExplicitPropertyChanged = "PropertyChanged";
    public static readonly string PredicateMethodName = "Predicate"; // bool Link name + PredicateMethodName()
    public static readonly string AddedMethodName = "Added"; // void Link name + AddedMethodName()
    public static readonly string RemovedMethodName = "Removed"; // void Link name + RemovedMethodName()
    public static readonly string GeneratedIdentifierName = "__gen_cl_identifier__";
    public static readonly string AddToGoshujinName = "AddToGoshujin";
    public static readonly string RemoveFromGoshujinName = "RemoveFromGoshujin";
    public static readonly string SetGoshujinName = "SetGoshujin";
    // public static readonly string GeneratedRemoveInternalName = "RemoveInternal";
    public static readonly string GeneratedNullLockName = "__gen_cl_null_lock__";
    public static readonly string GeneratedGoshujinLockName = "__gen_cl_gosh_lock__";
    public static readonly string WriterClassName = "WriterClass";
    public static readonly string TryLockMethodName = "TryLock";
    public static readonly string TryLockAsyncMethodName = "TryLockAsync";
    public static readonly string WriterSemaphoreName = "writerSemaphore";
    public static readonly string RepeatableObjectState = "RepeatableObjectState";
    public static readonly string IRepeatableObject = "IRepeatableObject";
    public static readonly string SerializableGoshujin = "SerializableGoshujin";
    public static readonly string RepeatableGoshujin = "RepeatableGoshujin";
    public static readonly string IValueLinkObjectInternal = "IValueLinkObjectInternal";
    public static readonly string ValueLinkInternalHelper = "ValueLinkInternalHelper";
    public static readonly string IRepeatableSemaphore = "ValueLink.IRepeatableSemaphore";
    public static readonly string ISerializableSemaphore = "ValueLink.ISerializableSemaphore";
    public static readonly string IIntegralityObject = "IIntegralityObject";
    public static readonly string IIntegralityGoshujin = "IIntegralityGoshujin";
    public static readonly string Integrality = "ValueLink.Integrality.IIntegralityInternal";
    public static readonly string KeyHashDictionaryName = "__keyhash_dictionary__";
    public static readonly string UnsafeConstructorName = "UnsafeConstructor";

    public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
        id: "CLG001", title: "Not a partial class/struct", messageFormat: "ValueLinkObject '{0}' is not a partial class/struct",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
        id: "CLG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
        id: "CLG003", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeywordUsed = new DiagnosticDescriptor(
        id: "CLG004", title: "Keyword used", messageFormat: "The type '{0}' already contains a definition for '{1}'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_ReadonlyMember = new DiagnosticDescriptor(
        id: "CLG005", title: "Readonly", messageFormat: "The the link target '{0}' cannot be set to read-only or getter-only unless AddValue is set to false",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LinkTargetNameError = new DiagnosticDescriptor(
        id: "CLG006", title: "Name error", messageFormat: "The field '{0}' to be linked must start with a lowercase letter",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MultipleLink = new DiagnosticDescriptor(
        id: "CLG007", title: "Link error", messageFormat: "One link is allowed per member, consider adding a LinkAttribute to a constructor",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoLinkTarget = new DiagnosticDescriptor(
        id: "CLG008", title: "Link error", messageFormat: "This type of link requires a property or field to be linked",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoNotifyTarget = new DiagnosticDescriptor(
        id: "CLG009", title: "Link error", messageFormat: "AutoNotify option requires a property or field to be linked",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LinkNameRequired = new DiagnosticDescriptor(
        id: "CLG010", title: "Link error", messageFormat: "Link name is required",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_PropertyChanged = new DiagnosticDescriptor(
        id: "CLG011", title: "INotifyPropertyChanged implementation", messageFormat: "{0} must implement INotifyPropertyChanged directly (PropertyChanged.Invoke() is not allowed to be called from inherited classes)",
        category: "ValueLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_StringKeySizeLimit = new DiagnosticDescriptor(
        id: "CLG012", title: "String key limit", messageFormat: "The size of the string key exceeds the limit {0}",
        category: "ValueLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_StringKeyConflict = new DiagnosticDescriptor(
        id: "CLG013", title: "String keys conflict", messageFormat: "String keys with the same name were detected",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_StringKeyNull = new DiagnosticDescriptor(
        id: "CLG014", title: "String key null", messageFormat: "String key cannot contain null character",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_NoPrimaryLink = new DiagnosticDescriptor(
        id: "CLG015", title: "No primary link", messageFormat: "Consider specifying a primary link that holds all objects in the collection in order to maximize the performance of serialization",
        category: "ValueLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_NoKeyAttribute = new DiagnosticDescriptor(
        id: "CLG016", title: "No Key attribute", messageFormat: "Consider adding Key or KeyAsName attribute to this member so that ValueLink can serialize it properly",
        category: "ValueLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DerivedClass = new DiagnosticDescriptor(
        id: "CLG017", title: "Derived class", messageFormat: "ValueLinkObject '{0}' cannot be derived from another ValueLinkObject '{1}'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_LinkMember = new DiagnosticDescriptor(
        id: "CLG018", title: "Link error", messageFormat: "Target member is only supported for LinkAttributes annotated in the constructor",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoTargetMember = new DiagnosticDescriptor(
        id: "CLG019", title: "No target member", messageFormat: "'{0}' does not contain a class member named '{1}'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MultipleLink2 = new DiagnosticDescriptor(
        id: "CLG020", title: "Multiple links", messageFormat: "Multiple links per member are not allowed",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InaccessibleMember = new DiagnosticDescriptor(
        id: "CLG021", title: "Inaccessible member", messageFormat: "'{0}' is inaccessible due to its protection level",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_AutoNotifyEnabled = new DiagnosticDescriptor(
        id: "CLG022", title: "AutoNotify enabled", messageFormat: "Value property is enabled for AutoNotify to work properly",
        category: "ValueLinkGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoUniqueLink2 = new DiagnosticDescriptor(
        id: "CLG023", title: "No unique link", messageFormat: "Unique link is required when the journaling feature is enabled",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MustBeRecord = new DiagnosticDescriptor(
        id: "CLG024", title: "Record required", messageFormat: "The target must be a record class when the isolation level is set to RepeatableRead",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeywordUsed2 = new DiagnosticDescriptor(
        id: "CLG025", title: "Keyword used", messageFormat: "Keyword '{0}' is reserved for source generator.'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoDefaultConstructor = new DiagnosticDescriptor(
        id: "CLG026", title: "No default constructor", messageFormat: "'{0}' must have a default constructor when the isolation level is repeatable read'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoUniqueLink = new DiagnosticDescriptor(
        id: "CLG027", title: "Unique required", messageFormat: "Unique link is required when the isolation level is set to RepeatableRead",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_UniqueLinkType = new DiagnosticDescriptor(
        id: "CLG028", title: "Unique link type", messageFormat: "The type of a unique link needs to be either ChainType.Ordered, ChainType.ReverseOrdered or ChainType.Unordered",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoChain = new DiagnosticDescriptor(
        id: "CLG029", title: "No chain", messageFormat: "There is no chain available for sharing",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_InconsistentType = new DiagnosticDescriptor(
        id: "CLG030", title: "Inconsistent type", messageFormat: "The type of members sharing the chain must be identical",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntegralityLink = new DiagnosticDescriptor(
        id: "CLG031", title: "Integrality link", messageFormat: "The integrality object must have a unique link, and its type must be a struct",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntegralityIsolation = new DiagnosticDescriptor(
        id: "CLG032", title: "Integrality isolation", messageFormat: "The IsolationLevel must be either 'IsolationLevel.None' or 'IsolationLevel.Serializable'",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IntegralityTinyhand = new DiagnosticDescriptor(
        id: "CLG033", title: "Integrality tinyhand", messageFormat: "The integrality object must have TinyhandObject attribute",
        category: "ValueLinkGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public ValueLinkBody(GeneratorExecutionContext context)
        : base(context)
    {
    }

    public ValueLinkBody(SourceProductionContext context)
        : base(context)
    {
    }

    internal Dictionary<string, List<ValueLinkObject>> Namespaces = new();

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

    public void Generate(IGeneratorInformation generator, CancellationToken cancellationToken)
    {
        ScopingStringBuilder ssb = new();
        GeneratorInformation info = new();
        List<ValueLinkObject> rootObjects = new();

        // Namespace
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tinyhandFlag = x.Value.Any(a => a.ContainTinyhandObjectAttribute()); // has TinyhandObjectAttribute
            this.GenerateHeader(ssb, tinyhandFlag);
            ssb.AppendNamespace(x.Key);

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
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.ValueLink.{x.Key}.cs"));
            }
            else
            {
                var hintName = $"gen.ValueLink.{x.Key}";
                var sourceText = SourceText.From(result, Encoding.UTF8);
                this.Context?.AddSource(hintName, sourceText);
                this.Context2?.AddSource(hintName, sourceText);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (info.UseTinyhand)
        {
            this.GenerateLoader(generator, info, rootObjects, this.Namespaces);
        }

        this.FlushDiagnostic();
    }

    private void GenerateHeader(ScopingStringBuilder ssb, bool tinyhandFlag)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Linq");
        ssb.AddUsing("System.Runtime.CompilerServices");
        ssb.AddUsing("System.Runtime.InteropServices");
        ssb.AddUsing("System.Threading");
        ssb.AddUsing("System.Threading.Tasks");
        ssb.AddUsing("Arc.Collections");
        ssb.AddUsing("Arc.Threading");
        ssb.AddUsing("ValueLink");
        ssb.AddUsing("ValueLink.Integrality");
        if (tinyhandFlag)
        {
            ssb.AddUsing("Tinyhand");
            ssb.AddUsing("Tinyhand.IO");
            ssb.AddUsing("Tinyhand.Resolvers");
        }

        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS0169", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        // ssb.AppendLine("#pragma warning disable SA1306", false); // Field names should begin with lower-case letter
        // ssb.AppendLine("#pragma warning disable SA1401", false); // Fields should be private
        ssb.AppendLine();
    }

    private void GenerateLoader(IGeneratorInformation generator, GeneratorInformation info, List<ValueLinkObject> rootObjects, Dictionary<string, List<ValueLinkObject>> namespaces)
    {
        var ssb = new ScopingStringBuilder();
        this.GenerateHeader(ssb, true);

        using (var scopeFormatter = ssb.ScopeNamespace("ValueLink.Generator"))
        {
            using (var methods = ssb.ScopeBrace("static class Generated"))
            {
                info.FinalizeBlock(ssb);

                // FlatLoader
                using (var m = ssb.ScopeBrace("internal static void __gen__cl()"))
                {
                    foreach (var x in namespaces.Values)
                    {
                        foreach (var y in x)
                        {
                            y.GenerateFlatLoader(ssb, info);
                        }
                    }
                }
            }
        }

        this.GenerateInitializer(generator, ssb, info);

        var result = ssb.Finalize();

        if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
        {
            this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.ValueLinkGenerated.cs"));
        }
        else
        {
            var hintName = "gen.ValueLinkLoader";
            var sourceText = SourceText.From(result, Encoding.UTF8);
            this.Context?.AddSource(hintName, sourceText);
            this.Context2?.AddSource(hintName, sourceText);
        }
    }

    private void GenerateInitializer(IGeneratorInformation generator, ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Namespace
        var ns = "ValueLink";
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

        info.ModuleInitializerClass.Add("ValueLink.Generator.Generated");

        ssb.AppendLine();
        using (var scopeValueLink = ssb.ScopeNamespace(ns!))
        using (var scopeClass = ssb.ScopeBrace("public static class ValueLinkModule" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

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
