// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Pure functions for path-scoped triggering: which repo-root-relative directories a Module watches, and the
/// composed closure hash over the reported tree hashes of those directories. The composed hash is a function of
/// both the membership list and the per-path hashes, so adding or removing a watched path moves it by design.
/// </summary>
public static class TriggerPathClosure
{
    /// <summary>
    /// Normalizes a repo-root-relative directory: trims whitespace and slashes; empty means the repository root
    /// and is represented as ".". Matches the runner-side normalization in BareCloneCache.
    /// </summary>
    public static string NormalizePath(string path)
    {
        var trimmed = path.Trim().Trim('/');
        return trimmed.Length == 0 ? "." : trimmed;
    }

    /// <summary>
    /// The Module's watched directory set: its SourceSubdirectory plus its own and its Namespace's additional
    /// trigger paths, normalized, deduplicated and ordinally sorted. Requires AdditionalTriggerPaths and
    /// Namespace.AdditionalTriggerPaths to be loaded.
    /// </summary>
    public static List<string> WatchedPaths(Module module)
    {
        return new[] { module.SourceSubdirectory }
            .Concat(module.AdditionalTriggerPaths.Select(p => p.Path))
            .Concat(module.Namespace?.AdditionalTriggerPaths.Select(p => p.Path) ?? Enumerable.Empty<string>())
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Whether path-scoped triggering is on for the Module: module flag, else namespace default, else false.
    /// </summary>
    public static bool FilterEnabled(Module module)
    {
        return module.TriggerPathFilterEnabled ?? module.Namespace?.DefaultTriggerPathFilterEnabled ?? false;
    }

    /// <summary>
    /// The watched-path union a refresh request must carry for one refresh group: the declared paths of every
    /// filter-enabled member. Every dispatch site must use this — the consumer evaluates all eligible members
    /// of the group against the report, and a report missing a member's paths composes with empty hashes and
    /// fail-opens into a spurious trigger.
    /// </summary>
    public static List<string> GroupWatchedPaths(IEnumerable<Module> groupMembers)
    {
        return groupMembers
            .Where(FilterEnabled)
            .SelectMany(WatchedPaths)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Widens the declared watched paths with the discovered reference closure of each: for every declared path
    /// that has a reported closure, its referenced paths join the set. Null closures (no discovery in this
    /// refresh) leave the declared set unchanged.
    /// </summary>
    public static List<string> ExpandWithClosures(IReadOnlyCollection<string> declaredPaths, IReadOnlyDictionary<string, List<string>>? closuresByRoot)
    {
        if (closuresByRoot == null) return declaredPaths.ToList();

        return declaredPaths
            .Concat(declaredPaths.SelectMany(p => closuresByRoot.GetValueOrDefault(p) ?? new List<string>()))
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Composes the closure hash over the watched paths from reported per-path tree hashes. A path missing from
    /// the report contributes an empty hash, which is also how the runner reports a directory that does not exist
    /// at the refreshed commit — either way the composition moves when a path appears or disappears.
    /// </summary>
    public static string Compose(IEnumerable<string> watchedPaths, IReadOnlyDictionary<string, string> reportedTreeHashes)
    {
        var preimage = string.Join('\n', watchedPaths
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => $"{p}\0{reportedTreeHashes.GetValueOrDefault(p, "")}"));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(preimage))).ToLowerInvariant();
    }
}
