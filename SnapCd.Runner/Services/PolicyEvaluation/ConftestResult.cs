// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Runner.Services.PolicyEvaluation;

public class ConftestViolation
{
    public required string Message { get; init; }

    /// <summary>The originating rule query, e.g. data.snapcd.policies.s3.deny — per-rule attribution.</summary>
    public string? Query { get; init; }

    /// <summary>Author-defined structured details (violation-rule payloads), serialized JSON.</summary>
    public string? DetailsJson { get; init; }
}

public class ConftestNamespaceResult
{
    public required string Namespace { get; init; }
    public int Successes { get; init; }
    public List<ConftestViolation> Failures { get; init; } = new();
    public List<ConftestViolation> Warnings { get; init; } = new();
}

public class ConftestParseException : Exception
{
    public ConftestParseException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public static class ConftestResultParser
{
    /// <summary>
    /// Parses `conftest test --output json` stdout. The failures/warnings arrays are the source of
    /// truth for severity — conftest already classified rule results by name (deny*/violation* vs
    /// warn*, prefix-matched); severity is never re-derived here.
    /// </summary>
    public static List<ConftestNamespaceResult> Parse(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException e)
        {
            throw new ConftestParseException("conftest output was not valid JSON", e);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new ConftestParseException($"conftest output was not a JSON array (got {doc.RootElement.ValueKind})");

            var results = new List<ConftestNamespaceResult>();
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                results.Add(new ConftestNamespaceResult
                {
                    Namespace = entry.TryGetProperty("namespace", out var ns) ? ns.GetString() ?? "" : "",
                    Successes = entry.TryGetProperty("successes", out var s) ? s.GetInt32() : 0,
                    Failures = ParseViolations(entry, "failures"),
                    Warnings = ParseViolations(entry, "warnings")
                });
            }

            return results;
        }
    }

    /// <summary>
    /// True when the evaluated policy defined no deny/violation/warn rules at all. A rule that
    /// evaluates clean still counts as a success, so all-zero across every namespace means the
    /// policy gates nothing (e.g. a typo'd rule name) — conftest itself exits 0 in that case.
    /// </summary>
    public static bool DefinesNoPolicyRules(List<ConftestNamespaceResult> results)
    {
        return results.Sum(r => r.Successes + r.Failures.Count + r.Warnings.Count) == 0;
    }

    private static List<ConftestViolation> ParseViolations(JsonElement entry, string property)
    {
        var list = new List<ConftestViolation>();
        if (!entry.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in array.EnumerateArray())
        {
            string? query = null;
            string? details = null;
            if (item.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
            {
                if (metadata.TryGetProperty("query", out var q)) query = q.GetString();
                if (metadata.TryGetProperty("details", out var d)) details = d.GetRawText();
            }

            list.Add(new ConftestViolation
            {
                Message = item.TryGetProperty("msg", out var msg) ? msg.GetString() ?? "" : "",
                Query = query,
                DetailsJson = details
            });
        }

        return list;
    }
}
