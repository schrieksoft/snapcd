// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IO.Compression;
using System.Text.Json;

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

/// <summary>
/// Reads a stored plan into a tree. The runner writes the engine's own machine-readable form of
/// the plan, gzipped; this is the only place that knows the stored shape.
/// </summary>
public static class PlanReader
{
    public static ParsedPlan? Read(byte[]? payload, DiffOptions? options = null)
    {
        if (payload is null || payload.Length == 0) return null;

        using var gzip = new GZipStream(new MemoryStream(payload), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        return Parse(reader.ReadToEnd(), options);
    }

    public static ParsedPlan Parse(string json, DiffOptions? options = null)
    {
        options ??= new DiffOptions();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var plan = new ParsedPlan
        {
            FormatVersion = Text(root, "format_version") ?? "",
            EngineVersion = Text(root, "terraform_version") ?? "",
            Errored = root.TryGetProperty("errored", out var e) && e.ValueKind == JsonValueKind.True
        };

        if (root.TryGetProperty("resource_changes", out var changes))
            foreach (var rc in changes.EnumerateArray())
                plan.Resources.Add(ReadResource(rc, options));

        if (root.TryGetProperty("output_changes", out var outputs))
            foreach (var oc in outputs.EnumerateObject())
                plan.Outputs.Add(ReadOutput(oc, options));

        return plan;
    }

    private static ResourceDiff ReadResource(JsonElement rc, DiffOptions options)
    {
        var change = rc.GetProperty("change");

        return new ResourceDiff
        {
            Address = Text(rc, "address") ?? "",
            Type = Text(rc, "type") ?? "",
            ProviderName = Text(rc, "provider_name") ?? "",
            Action = ActionOf(change),
            ActionReason = Text(rc, "action_reason"),
            Root = StructuralDiff.Build(
                Element(change, "before"),
                Element(change, "after"),
                Element(change, "after_unknown"),
                Element(change, "after_sensitive"),
                options)
        };
    }

    private static OutputDiff ReadOutput(JsonProperty oc, DiffOptions options)
    {
        var change = oc.Value;

        return new OutputDiff
        {
            Name = oc.Name,
            Action = ActionOf(change),
            Sensitive = change.TryGetProperty("after_sensitive", out var s) && s.ValueKind == JsonValueKind.True,
            Root = StructuralDiff.Build(
                Element(change, "before"),
                Element(change, "after"),
                Element(change, "after_unknown"),
                Element(change, "after_sensitive"),
                options)
        };
    }

    /// <summary>
    /// The actions array carries one verb, except for a replacement, where it carries the delete
    /// and the create in the order they happen.
    /// </summary>
    private static ResourceAction ActionOf(JsonElement change)
    {
        if (!change.TryGetProperty("actions", out var actions) || actions.ValueKind != JsonValueKind.Array)
            return ResourceAction.NoOp;

        var verbs = actions.EnumerateArray().Select(a => a.GetString()).ToList();

        if (verbs.Count > 1) return ResourceAction.Replace;

        return verbs.FirstOrDefault() switch
        {
            "create" => ResourceAction.Create,
            "read" => ResourceAction.Read,
            "update" => ResourceAction.Update,
            "delete" => ResourceAction.Delete,
            "forget" => ResourceAction.Forget,
            _ => ResourceAction.NoOp
        };
    }

    private static string? Text(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static JsonElement? Element(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.Clone() : null;
}
