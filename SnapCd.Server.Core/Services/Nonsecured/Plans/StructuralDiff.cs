// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

public sealed class DiffOptions
{
    /// <summary>
    /// Compare object keys regardless of the order they were written in. A map is unordered, so a
    /// reordering is not a change, and every text comparison reports one anyway.
    /// </summary>
    public bool NormaliseKeyOrder { get; init; } = true;

    /// <summary>
    /// Parse a string that holds a YAML or JSON document and walk into it. This is what reaches
    /// the inside of a values blob, which the engine treats as one opaque string.
    /// </summary>
    public bool DescendIntoEmbedded { get; init; } = true;

    /// <summary>
    /// Paths that identify an element of an array, tried in order and written dot separated so a
    /// nested identity is reachable. Pairing by identity rather than position is what stops an
    /// insertion reading as every later element having changed.
    /// </summary>
    public IReadOnlyList<string> IdentityPaths { get; init; } =
        ["name", "id", "key", "Name", "ID", "metadata.name", "metadata.Name"];

    /// <summary>
    /// Diff a multi-line string line by line where it holds no structure. A certificate, a script
    /// or a block of free text still reads better as a diff than as two whole values.
    /// </summary>
    public bool LineDiffPlainText { get; init; } = true;
}

/// <summary>
/// Walks a before and an after value together and produces one tree. The walk never stops at a
/// container: an array element is paired and then descended into, which is the one thing the
/// engine's renderer declines to do.
/// </summary>
public static class StructuralDiff
{
    public static DiffNode Build(
        JsonElement? before,
        JsonElement? after,
        JsonElement? unknown = null,
        JsonElement? sensitive = null,
        DiffOptions? options = null)
        => Walk("", "", before, after, unknown, sensitive, options ?? new DiffOptions());

    private static DiffNode Walk(
        string name,
        string path,
        JsonElement? before,
        JsonElement? after,
        JsonElement? unknown,
        JsonElement? sensitive,
        DiffOptions options)
    {
        var isUnknown = IsTrue(unknown);
        var isSensitive = IsTrue(sensitive);

        // A value the plan cannot predict has no after to compare, so the walk stops here and the
        // node says why rather than showing a null.
        if (isUnknown)
            return new DiffNode
            {
                Name = name,
                Path = path,
                Kind = KindOf(before),
                Action = before is null ? NodeAction.Added : NodeAction.Changed,
                Before = isSensitive ? null : before,
                UnknownAfterApply = true,
                Sensitive = isSensitive
            };

        if (before is null && after is null)
            return new DiffNode { Name = name, Path = path, Kind = NodeKind.Null, Action = NodeAction.Unchanged };

        if (before is null)
            return isSensitive
                ? new DiffNode { Name = name, Path = path, Kind = KindOf(after), Action = NodeAction.Added, Sensitive = true }
                : Leaf(name, path, null, after, NodeAction.Added, false);

        if (after is null)
            return isSensitive
                ? new DiffNode { Name = name, Path = path, Kind = KindOf(before), Action = NodeAction.Removed, Sensitive = true }
                : Leaf(name, path, before, null, NodeAction.Removed, false);

        var b = before.Value;
        var a = after.Value;

        // A sensitive value is compared but never carried: the node says whether it changed and
        // nothing else. Marking the node and leaving the value on it would put a secret one
        // forgotten condition away from the screen.
        if (isSensitive)
            return new DiffNode
            {
                Name = name,
                Path = path,
                Kind = KindOf(b),
                Action = Same(b, a) ? NodeAction.Unchanged : NodeAction.Changed,
                Sensitive = true
            };

        // Two different kinds cannot be walked together, so the node is a replacement.
        if (KindOf(b) != KindOf(a))
            return Leaf(name, path, b, a, NodeAction.Replaced, isSensitive);

        return KindOf(b) switch
        {
            NodeKind.Object => WalkObject(name, path, b, a, unknown, sensitive, options, isSensitive),
            NodeKind.Array => WalkArray(name, path, b, a, unknown, sensitive, options, isSensitive),
            NodeKind.String => WalkString(name, path, b, a, options, isSensitive),
            _ => Leaf(name, path, b, a, Same(b, a) ? NodeAction.Unchanged : NodeAction.Changed, isSensitive)
        };
    }

    private static DiffNode WalkObject(
        string name, string path, JsonElement b, JsonElement a,
        JsonElement? unknown, JsonElement? sensitive, DiffOptions options, bool isSensitive)
    {
        var node = new DiffNode
        {
            Name = name, Path = path, Kind = NodeKind.Object,
            Action = NodeAction.Unchanged, Before = b, After = a, Sensitive = isSensitive
        };

        // A map is unordered, so the union of the keys is walked rather than either side's order.
        var keys = b.EnumerateObject().Select(p => p.Name)
            .Union(a.EnumerateObject().Select(p => p.Name));

        if (options.NormaliseKeyOrder) keys = keys.OrderBy(k => k, StringComparer.Ordinal);

        foreach (var key in keys)
        {
            var child = Walk(
                key,
                path.Length == 0 ? key : $"{path}.{key}",
                Property(b, key), Property(a, key),
                Property(unknown, key), Property(sensitive, key),
                options);

            node.Children.Add(child);
        }

        if (node.Children.Any(c => c.HasChange)) node.Action = NodeAction.Changed;
        return node;
    }

    private static DiffNode WalkArray(
        string name, string path, JsonElement b, JsonElement a,
        JsonElement? unknown, JsonElement? sensitive, DiffOptions options, bool isSensitive)
    {
        var node = new DiffNode
        {
            Name = name, Path = path, Kind = NodeKind.Array,
            Action = NodeAction.Unchanged, Before = b, After = a, Sensitive = isSensitive
        };

        var beforeItems = b.EnumerateArray().ToList();
        var afterItems = a.EnumerateArray().ToList();

        var (strategy, matches) = ArrayPairing.Pair(beforeItems, afterItems, options);

        foreach (var match in matches)
        {
            // An identity-paired element is named by its identity, so a reader sees which one it is
            // rather than a position that may have moved.
            var label = strategy == ArrayPairing.Strategy.Identity ? match.Label : $"[{match.Index}]";

            var child = Walk(
                label,
                $"{path}[{match.Index}]",
                match.Before, match.After,
                Index(unknown, match.Index), Index(sensitive, match.Index),
                options);

            node.Children.Add(child);
        }

        if (node.Children.Any(c => c.HasChange)) node.Action = NodeAction.Changed;
        return node;
    }

    /// <summary>
    /// A string, which may hold a whole document. Where both sides parse as the same format, the
    /// walk continues inside it, which is how a values blob becomes a tree rather than one opaque
    /// value. The node keeps the raw strings so a line diff is still available.
    /// </summary>
    private static DiffNode WalkString(
        string name, string path, JsonElement b, JsonElement a, DiffOptions options, bool isSensitive)
    {
        var bs = b.GetString() ?? "";
        var @as = a.GetString() ?? "";

        if (bs == @as)
            return Leaf(name, path, b, a, NodeAction.Unchanged, isSensitive);

        if (options.DescendIntoEmbedded
            && EmbeddedDocument.TryParse(bs, out var bDoc, out var format)
            && EmbeddedDocument.TryParse(@as, out var aDoc, out var aFormat)
            && format == aFormat)
        {
            var inner = Walk(name, path, bDoc, aDoc, null, null, options);

            var node = new DiffNode
            {
                Name = name, Path = path, Kind = inner.Kind,
                Action = inner.Action, Before = b, After = a,
                Embedded = true, EmbeddedFormat = format, Sensitive = isSensitive
            };

            node.Children.AddRange(inner.Children);
            return node;
        }

        // No structure, but still more than one line: a certificate, a script, a block of text.
        // The node carries both values and is marked so a renderer shows a line diff rather than
        // two whole blobs.
        var multiline = options.LineDiffPlainText && (bs.Contains('\n') || @as.Contains('\n'));

        return new DiffNode
        {
            Name = name, Path = path, Kind = NodeKind.String,
            Action = NodeAction.Changed, Before = b, After = a,
            Multiline = multiline, Sensitive = isSensitive
        };
    }

    private static DiffNode Leaf(
        string name, string path, JsonElement? before, JsonElement? after,
        NodeAction action, bool isSensitive) => new()
    {
        Name = name,
        Path = path,
        Kind = KindOf(after ?? before),
        Action = action,
        Before = before,
        After = after,
        Sensitive = isSensitive
    };

    private static bool Same(JsonElement a, JsonElement b) => a.GetRawText() == b.GetRawText();

    private static NodeKind KindOf(JsonElement? e) => e?.ValueKind switch
    {
        JsonValueKind.Object => NodeKind.Object,
        JsonValueKind.Array => NodeKind.Array,
        JsonValueKind.String => NodeKind.String,
        JsonValueKind.Number => NodeKind.Number,
        JsonValueKind.True or JsonValueKind.False => NodeKind.Bool,
        _ => NodeKind.Null
    };

    private static JsonElement? Property(JsonElement? e, string name)
        => e is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty(name, out var v) ? v : null;

    private static JsonElement? Index(JsonElement? e, int index)
        => e is { ValueKind: JsonValueKind.Array } a && index < a.GetArrayLength()
            ? a[index]
            : null;

    /// <summary>
    /// The unknown and sensitive trees mirror the value's shape, with true at a marked position.
    /// A true anywhere above a leaf marks everything beneath it.
    /// </summary>
    private static bool IsTrue(JsonElement? e) => e is { ValueKind: JsonValueKind.True };
}
