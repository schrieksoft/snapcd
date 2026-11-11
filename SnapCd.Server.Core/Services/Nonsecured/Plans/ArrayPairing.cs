// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

/// <summary>
/// Decides which element of a before array corresponds to which element of an after array.
///
/// This is the whole difficulty of diffing a collection. Pairing by position is what makes an
/// insertion read as every later element having changed, which is the cascade that makes a plan
/// unreadable. Three strategies, tried in order of how much they can prove.
/// </summary>
public static class ArrayPairing
{
    public enum Strategy
    {
        /// <summary>Elements carry a stable identity, so a move is not a change.</summary>
        Identity,

        /// <summary>No identity, so the longest common subsequence finds the real edits.</summary>
        Lcs,

        /// <summary>Nothing better applies.</summary>
        Positional
    }

    public sealed record Match(JsonElement? Before, JsonElement? After, int Index, string Label);

    public static (Strategy Strategy, List<Match> Matches) Pair(
        List<JsonElement> before, List<JsonElement> after, DiffOptions options)
    {
        var key = IdentityKey(before, after, options);
        if (key is not null) return (Strategy.Identity, ByIdentity(before, after, key));

        return (Strategy.Lcs, ByLcs(before, after));
    }

    /// <summary>
    /// The first identity path every element on both sides carries. A path is used rather than a
    /// key so that a Kubernetes object, whose name sits at metadata.name, can be matched as
    /// readily as one with a name at the top.
    /// </summary>
    private static string[]? IdentityKey(List<JsonElement> before, List<JsonElement> after, DiffOptions options)
    {
        var all = before.Concat(after).ToList();

        if (all.Count == 0 || all.Any(e => e.ValueKind != JsonValueKind.Object))
            return null;

        foreach (var candidate in options.IdentityPaths)
        {
            var path = candidate.Split('.');
            if (all.All(e => Resolve(e, path) is { ValueKind: JsonValueKind.String or JsonValueKind.Number }))
                return path;
        }

        return null;
    }

    private static JsonElement? Resolve(JsonElement e, string[] path)
    {
        JsonElement? current = e;

        foreach (var step in path)
        {
            if (current is not { ValueKind: JsonValueKind.Object } o || !o.TryGetProperty(step, out var next))
                return null;

            current = next;
        }

        return current;
    }

    private static List<Match> ByIdentity(List<JsonElement> before, List<JsonElement> after, string[] key)
    {
        var pairs = new List<Match>();
        var afterById = new Dictionary<string, JsonElement>();

        foreach (var item in after)
            if (Resolve(item, key)?.ToString() is { } id)
                afterById[id] = item;

        var matched = new HashSet<string>();
        var index = 0;

        foreach (var item in before)
        {
            var id = Resolve(item, key)?.ToString();

            if (id is not null && afterById.TryGetValue(id, out var counterpart))
            {
                matched.Add(id);
                pairs.Add(new Match(item, counterpart, index++, id));
            }
            else
            {
                pairs.Add(new Match(item, null, index++, id ?? $"[{index}]"));
            }
        }

        foreach (var item in after)
        {
            var id = Resolve(item, key)?.ToString();
            if (id is null || matched.Contains(id)) continue;
            pairs.Add(new Match(null, item, index++, id));
        }

        return pairs;
    }

    /// <summary>
    /// Longest common subsequence over the elements' raw text. An element that is deeply equal is
    /// an anchor, and what falls between anchors is a real insertion or removal rather than a
    /// shifted comparison. Elements that align between two anchors are still paired, so a changed
    /// element is descended into rather than reported as a removal and an addition.
    /// </summary>
    private static List<Match> ByLcs(List<JsonElement> before, List<JsonElement> after)
    {
        var b = before.Select(e => e.GetRawText()).ToList();
        var a = after.Select(e => e.GetRawText()).ToList();

        var lcs = new int[b.Count + 1, a.Count + 1];

        for (var i = b.Count - 1; i >= 0; i--)
        for (var j = a.Count - 1; j >= 0; j--)
            lcs[i, j] = b[i] == a[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        var pairs = new List<Match>();
        var removed = new List<JsonElement>();
        var added = new List<JsonElement>();
        int x = 0, y = 0, index = 0;

        void Flush()
        {
            // A removal and an addition at the same spot is one element changing, so the two are
            // paired and walked rather than reported separately.
            var shared = Math.Min(removed.Count, added.Count);

            for (var k = 0; k < shared; k++)
                pairs.Add(new Match(removed[k], added[k], index, $"[{index++}]"));

            for (var k = shared; k < removed.Count; k++)
                pairs.Add(new Match(removed[k], null, index, $"[{index++}]"));

            for (var k = shared; k < added.Count; k++)
                pairs.Add(new Match(null, added[k], index, $"[{index++}]"));

            removed.Clear();
            added.Clear();
        }

        while (x < b.Count && y < a.Count)
        {
            if (b[x] == a[y])
            {
                Flush();
                pairs.Add(new Match(before[x], after[y], index, $"[{index++}]"));
                x++;
                y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                removed.Add(before[x++]);
            }
            else
            {
                added.Add(after[y++]);
            }
        }

        while (x < b.Count) removed.Add(before[x++]);
        while (y < a.Count) added.Add(after[y++]);
        Flush();

        return pairs;
    }
}
