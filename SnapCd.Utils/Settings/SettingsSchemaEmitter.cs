// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using SnapCd.Contracts.Validation;

namespace SnapCd.Utils.Settings;

/// <summary>
/// Emits JSON Schema for SnapCD settings POCO types using the BCL <see cref="JsonSchemaExporter"/>,
/// augmented with two corrections the BCL output lacks out of the box:
///
/// 1. <b>Defaults</b> — <see cref="JsonSchemaExporter"/> does not infer the <c>default</c> keyword from
///    C# property initializers (initializers are constructor-body code, not type metadata). This emitter
///    instantiates each settings type via its parameterless constructor and reflects the property value,
///    injecting it as JSON Schema <c>default</c> when the value is a leaf-displayable type (primitive,
///    string, enum, decimal) and is not the CLR default for that type (so an unset <c>int</c> is treated
///    as "no default" rather than emitting <c>"default": 0</c>).
///
/// 2. <b>XML doc summaries</b> — pulled from the project's XML doc file when present and projected into
///    the schema's <c>description</c> keyword. Falls back to <see cref="System.ComponentModel.DescriptionAttribute"/>
///    when no XML doc file is loaded for the declaring assembly.
///
/// The intended consumers are per-component CLI generators (<c>SnapCd.Settings.Generator.*</c>) that
/// write the resulting JSON to <c>applications/snapcd/schemas/&lt;component&gt;.schema.json</c> and a
/// pre-commit/CI hook that runs the generators in <c>--check</c> mode to flag drift.
/// </summary>
public static class SettingsSchemaEmitter
{
    private static readonly JsonSerializerOptions SerializerOptions = BuildSerializerOptions();

    private static JsonSerializerOptions BuildSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            // Bumped from the BCL default of 64. Settings types that reference foreign SDK types
            // (DefaultAzureCredentialOptions, MassTransit transport options, etc.) can hit 64 layers
            // of nested-type metadata long before they hit anything resembling a runtime concern.
            // 256 absorbs every real case in the snapcd settings tree without masking true recursion.
            MaxDepth = 256,
        };

        // Enums in appsettings.json are written as strings (`"BusType": "SqlServer"`), not integers,
        // because Microsoft.Extensions.Configuration.Binder accepts string-named enum values out of
        // the box. JsonStringEnumConverter teaches the schema exporter the same convention, so the
        // emitted schema describes the enum field as a string with an `enum` array of allowed names
        // rather than an integer.
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    /// <summary>
    /// Emits JSON Schema for one settings type with defaults and XML doc descriptions injected.
    /// </summary>
    /// <param name="settingsType">The settings POCO type. Must have a parameterless constructor.</param>
    /// <param name="xmlDoc">Optional XML doc lookup, typically built once per assembly via <see cref="LoadXmlDoc"/>.</param>
    public static JsonNode Emit(Type settingsType, XmlDocLookup? xmlDoc = null)
    {
        var options = BuildExporterOptions(xmlDoc);
        return JsonSchemaExporter.GetJsonSchemaAsNode(SerializerOptions, settingsType, options);
    }

    /// <summary>
    /// Emits a single JSON Schema document combining multiple settings types as named sections under
    /// <c>properties</c>. The resulting document is the canonical artifact for a single component's
    /// <c>appsettings.json</c> surface and is what operators reference via the <c>$schema</c> directive.
    /// </summary>
    /// <param name="component">Component identifier, e.g. <c>"runner"</c>. Used for <c>$id</c> and <c>title</c>.</param>
    /// <param name="sectionTypes">Map of top-level section name (the key under which it appears in
    /// <c>appsettings.json</c>) to the POCO type. For example <c>{ "Runner", typeof(RunnerSettings) }</c>.</param>
    /// <param name="sectionFragments">Optional map of section name to a pre-built schema fragment.
    /// Used for sections whose shape isn't defined by a snapcd-owned POCO — for example
    /// <c>Logging</c> (standard .NET shape) via <see cref="StandardSchemaFragments.Logging"/>.
    /// Fragments are merged into the document's <c>properties</c> alongside the POCO-derived entries.
    /// A fragment with the same key as a POCO section wins (the fragment is the authority for
    /// that section's shape).</param>
    /// <param name="xmlDoc">Optional XML doc lookup.</param>
    public static JsonObject EmitDocument(
        string component,
        IReadOnlyDictionary<string, Type> sectionTypes,
        IReadOnlyDictionary<string, JsonNode>? sectionFragments = null,
        XmlDocLookup? xmlDoc = null)
    {
        var properties = new JsonObject();
        foreach (var (sectionName, type) in sectionTypes)
        {
            properties[sectionName] = Emit(type, xmlDoc);
        }

        // Fragments applied after POCO sections so a fragment with the same key wins. Each fragment
        // is deep-cloned because JsonObject keys can only have a single parent, and Lazy-cached
        // fragments would otherwise throw on the second generator invocation.
        if (sectionFragments is not null)
        {
            foreach (var (sectionName, fragment) in sectionFragments)
            {
                properties[sectionName] = fragment.DeepClone();
            }
        }

        return new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = $"https://docs.snapcd.io/schemas/{component}/latest/appsettings-schema.json",
            ["title"] = $"SnapCD {component} settings",
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = true,
        };
    }

    /// <summary>
    /// Loads the XML doc file produced alongside the given assembly when
    /// <c>&lt;GenerateDocumentationFile&gt;true&lt;/GenerateDocumentationFile&gt;</c> is set in its csproj.
    /// Returns <c>null</c> if no XML doc file is found next to the assembly.
    /// </summary>
    public static XmlDocLookup? LoadXmlDoc(Assembly assembly)
    {
        var assemblyPath = assembly.Location;
        if (string.IsNullOrEmpty(assemblyPath)) return null;

        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(xmlPath)) return null;

        return XmlDocLookup.Load(xmlPath);
    }

    private static JsonSchemaExporterOptions BuildExporterOptions(XmlDocLookup? xmlDoc)
    {
        return new JsonSchemaExporterOptions
        {
            // Treat null-oblivious reference types (no nullable annotation in source) as non-nullable.
            // Without this, every nested settings type's schema becomes `"type": ["object", "null"]`
            // because the BCL plays it safe in null-oblivious contexts — but settings POCOs are
            // never expected to be null at the section level, so the noise outweighs the safety.
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (ctx, schema) =>
            {
                if (schema is not JsonObject obj) return schema;

                // Project XML doc / [Description] into the description keyword. Property-level
                // context wins when present; otherwise fall through to the type-level summary so
                // a class-level `/// <summary>` on a settings POCO appears against the section
                // it's bound to in appsettings.json.
                if (ctx.PropertyInfo?.AttributeProvider is PropertyInfo prop)
                {
                    var description = ResolveDescription(prop, xmlDoc);
                    if (description is not null) obj["description"] = description;

                    InjectDefault(obj, prop);
                    InjectRequired(obj, prop.PropertyType);
                }
                else
                {
                    var description = ResolveDescription(ctx.TypeInfo.Type, xmlDoc);
                    if (description is not null) obj["description"] = description;

                    InjectRequired(obj, ctx.TypeInfo.Type);
                }

                return schema;
            },
        };
    }

    private static string? ResolveDescription(MemberInfo member, XmlDocLookup? xmlDoc)
    {
        var fromXml = xmlDoc?.GetSummary(member);
        if (!string.IsNullOrWhiteSpace(fromXml)) return fromXml;

        var descAttr = member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return descAttr?.Description;
    }

    private static void InjectDefault(JsonObject schema, PropertyInfo prop)
    {
        var declaring = prop.DeclaringType;
        if (declaring is null) return;
        if (declaring.GetConstructor(Type.EmptyTypes) is null) return;

        object? instance;
        try
        {
            instance = Activator.CreateInstance(declaring);
        }
        catch
        {
            return; // Type can't be instantiated cleanly — skip rather than fail the whole emit.
        }

        if (instance is null) return;

        object? value;
        try
        {
            value = prop.GetValue(instance);
        }
        catch
        {
            return;
        }

        if (!IsDisplayableDefault(value, prop.PropertyType)) return;

        // Serialize through JsonSerializer using the emitter's SerializerOptions (which include
        // JsonStringEnumConverter) so enum defaults appear as string names rather than as the
        // integer index a bare SerializeToNode would emit.
        var json = JsonSerializer.SerializeToNode(value, prop.PropertyType, SerializerOptions);
        if (json is not null) schema["default"] = json;
    }

    private static bool IsDisplayableDefault(object? value, Type type)
    {
        if (value is null) return false;

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsEnum) return true;
        if (underlying == typeof(string)) return !string.IsNullOrEmpty((string)value);
        if (underlying == typeof(decimal)) return (decimal)value != 0m;
        if (underlying.IsPrimitive)
        {
            // Skip CLR defaults for value types. A property declared `public int X { get; set; }` with
            // no initializer reflects as 0 — indistinguishable at runtime from an intentional `= 0`.
            // Treating both as "no default" is the right behaviour 95% of the time; the rare property
            // that intentionally defaults to a CLR-default value can carry [DefaultValue] (TODO: read
            // that as an override here if needed).
            var clrDefault = Activator.CreateInstance(underlying);
            return !value.Equals(clrDefault);
        }

        // Object types, collections, dictionaries: skip parent-level default. The recursive callback
        // emits defaults at the nested level when each sub-property's schema is generated.
        return false;
    }

    private static void InjectRequired(JsonObject schema, Type type)
    {
        var requiredProps = new JsonArray();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.IsDefined(typeof(RequiredAttribute), inherit: true)
                || prop.IsDefined(typeof(NonEmptyGuidAttribute), inherit: true))
            {
                requiredProps.Add(prop.Name);
            }
        }

        if (requiredProps.Count > 0)
            schema["required"] = requiredProps;
    }
}
