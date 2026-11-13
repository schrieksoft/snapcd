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

namespace SnapCd.Server.Core.Services.Utils;

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
    public static List<string> WatchedPaths(Module module, IReadOnlyList<Namespace>? chain = null)
    {
        var inherited = Inheritable(module, chain)
            .SelectMany(ns => ns.AdditionalTriggerPaths.Select(p => p.Path));

        return new[] { module.SourceSubdirectory }
            .Concat(module.AdditionalTriggerPaths.Select(p => p.Path))
            .Concat(inherited)
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Namespace>? ChainFor(Module module, IReadOnlyDictionary<Guid, List<Namespace>>? chains) =>
        chains != null && chains.TryGetValue(module.NamespaceId, out var chain) ? chain : null;

    /// <summary>
    /// The Namespaces a Module takes trigger paths from: the chain where one was loaded, else its own
    /// Namespace alone. Stops at a Module or Namespace that refuses what is above it.
    /// </summary>
    private static IEnumerable<Namespace> Inheritable(Module module, IReadOnlyList<Namespace>? chain)
    {
        if (module.IgnoreInheritedTriggerPaths) yield break;

        if (chain == null)
        {
            if (module.Namespace != null) yield return module.Namespace;
            yield break;
        }

        foreach (var ns in chain)
        {
            yield return ns;
            if (ns.IgnoreInheritedTriggerPaths) yield break;
        }
    }

    /// <summary>
    /// Whether path-scoped triggering is on for the Module: module flag, else namespace default, else false.
    /// </summary>
    public static bool FilterEnabled(Module module, IReadOnlyList<Namespace>? chain = null)
    {
        if (module.TriggerPathFilterEnabled is { } own) return own;

        var inherited = chain ?? (module.Namespace == null ? [] : new List<Namespace> { module.Namespace });

        bool? nearest = null;

        foreach (var ns in inherited)
        {
            nearest ??= ns.DefaultTriggerPathFilterEnabled;

            // Furthest pin wins, matching how the other inherited defaults resolve.
            if (ns.RestrictOverrideOnTriggerPathFilterEnabled == true && ns.DefaultTriggerPathFilterEnabled is { } pinned)
                nearest = pinned;
        }

        return nearest ?? false;
    }

    /// <summary>
    /// The watched-path union a refresh request must carry for one refresh group: the declared paths of every
    /// filter-enabled member. Every dispatch site must use this: the consumer judges a filter-enabled module only
    /// by a report that hashed all of its declared paths, so a member whose paths are left out is never triggered.
    /// </summary>
    public static List<string> GroupWatchedPaths(
        IEnumerable<Module> groupMembers,
        IReadOnlyDictionary<Guid, List<Namespace>>? chains = null)
    {
        return groupMembers
            .Where(m => FilterEnabled(m, ChainFor(m, chains)))
            .SelectMany(m => WatchedPaths(m, ChainFor(m, chains)))
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
    /// Composes the closure hash over the watched paths from reported per-path tree hashes. The runner reports a
    /// directory that does not exist at the refreshed commit as an empty hash, so the composition moves when a
    /// path appears or disappears.
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
