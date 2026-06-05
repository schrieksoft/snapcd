// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace SnapCd.Utils.Settings;

/// <summary>
/// Boilerplate for the per-component settings-schema CLI generators. Each generator's <c>Program.cs</c>
/// supplies the component name, the section-name → settings-type map, and the output path; this
/// runner handles the rest:
///
/// <list type="bullet">
///   <item>Builds the XML doc lookup off the assembly containing the settings types.</item>
///   <item>Calls <see cref="SettingsSchemaEmitter.EmitDocument"/> to produce the JSON.</item>
///   <item>In default (write) mode, writes the JSON to disk.</item>
///   <item>In <c>--check</c> mode, compares against the file on disk and exits non-zero on drift,
///         printing a diff-style message for the pre-commit / CI hook.</item>
/// </list>
///
/// Mirrors the operational model of <c>scripts/check-mcp-surface.sh</c>: a single binary that
/// regenerates or verifies, depending on the flag.
/// </summary>
public static class SettingsSchemaCliRunner
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Entry point invoked by each generator's <c>Program.cs</c>. Returns the process exit code.
    /// </summary>
    /// <param name="component">Component identifier — used for <c>$id</c> and <c>title</c>.</param>
    /// <param name="sectionTypes">Map of top-level <c>appsettings.json</c> section name to its POCO type.</param>
    /// <param name="outputPath">Absolute path of the schema file the generator owns.</param>
    /// <param name="args">The raw program args. Accepts <c>--check</c> to switch to verification mode.</param>
    /// <param name="sectionFragments">Optional map of section name to a pre-built schema fragment,
    /// forwarded to <see cref="SettingsSchemaEmitter.EmitDocument"/>. Use for standard .NET sections
    /// (e.g. Logging) and any other shape not owned by a snapcd POCO.</param>
    /// <param name="postProcess">Optional callback that receives the assembled schema document
    /// just before serialisation, so the generator can inject hand-authored fragments at nested
    /// paths. Use for properties whose runtime type comes from a foreign SDK (MassTransit's
    /// AzureServiceBusTransportOptions / SqlTransportOptions, etc.) — those properties carry
    /// [JsonIgnore] in source to prevent the BCL exporter from recursing into them, and the
    /// hand-authored schema is pasted in here.</param>
    public static int Run(
        string component,
        IReadOnlyDictionary<string, Type> sectionTypes,
        string outputPath,
        string[] args,
        IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>? sectionFragments = null,
        Action<System.Text.Json.Nodes.JsonObject>? postProcess = null)
    {
        var checkOnly = args.Any(a => a == "--check");

        // The settings types come from one assembly per component (the host project that owns them).
        // Pick the first one and load its XML doc; if a component ever splits settings across multiple
        // assemblies, extend this to merge lookups.
        var sourceAssembly = sectionTypes.Values.First().Assembly;
        var xmlDoc = SettingsSchemaEmitter.LoadXmlDoc(sourceAssembly);
        if (xmlDoc is null)
        {
            Console.Error.WriteLine(
                $"warning: no XML doc file found next to {sourceAssembly.GetName().Name}.dll — "
                + "descriptions will fall back to [Description] attributes only. "
                + "Set <GenerateDocumentationFile>true</GenerateDocumentationFile> in the host csproj.");
        }

        var emitted = SettingsSchemaEmitter.EmitDocument(component, sectionTypes, sectionFragments, xmlDoc);
        postProcess?.Invoke(emitted);
        var emittedJson = emitted.ToJsonString(WriteOptions) + Environment.NewLine;

        if (checkOnly)
        {
            return RunCheck(outputPath, emittedJson);
        }

        return RunWrite(outputPath, emittedJson);
    }

    private static int RunWrite(string outputPath, string emittedJson)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(outputPath, emittedJson);
        Console.Out.WriteLine($"Wrote {outputPath}");
        return 0;
    }

    private static int RunCheck(string outputPath, string emittedJson)
    {
        if (!File.Exists(outputPath))
        {
            Console.Error.WriteLine(
                $"error: schema file missing — {outputPath} does not exist. "
                + "Run the generator without --check to create it.");
            return 1;
        }

        var existing = File.ReadAllText(outputPath);
        if (string.Equals(existing, emittedJson, StringComparison.Ordinal))
        {
            return 0;
        }

        Console.Error.WriteLine(
            $"error: {outputPath} is out of date. "
            + "Run the generator without --check to regenerate, commit the result, and retry.");
        return 1;
    }
}
