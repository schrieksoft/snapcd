// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Runner.Services.SplitMonolith;

/// <summary>
/// Reads the single JSON document demonolith emits under --output json. Only statistics and the
/// shape of the carve are read: receipts stay on the runner.
/// </summary>
public static class DemonolithOutput
{
    private static JsonElement? Root(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;

        // The document is the last thing written; anything before it is progress output.
        var start = output.LastIndexOf('{');
        if (start < 0) return null;

        try
        {
            return JsonDocument.Parse(output[start..]).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? ReadString(string output, string property) =>
        Root(output) is { } root && root.TryGetProperty(property, out var value)
            ? value.GetString()
            : null;

    public static int ReadInt(string output, string property) =>
        Root(output) is { } root && root.TryGetProperty(property, out var value) && value.TryGetInt32(out var number)
            ? number
            : 0;

    public static List<string> ReadStringList(string output, string property)
    {
        if (Root(output) is not { } root
            || !root.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.Array)
            return [];

        return value.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }
}
