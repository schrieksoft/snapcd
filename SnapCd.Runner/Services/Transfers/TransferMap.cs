// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using YamlDotNet.Serialization;
using SnapCd.Contracts.RunnerRequests.Transfers;
using YamlDotNet.Serialization.NamingConventions;

namespace SnapCd.Runner.Services.Transfers;

/// <summary>
/// The committed transfer map, read from this root's own checkout. Only the cross-boundary wiring
/// is read here; everything else in the map is demonolith's business.
/// </summary>
public static class TransferMap
{
    /// <summary>The map format this runner reads. demonolith refuses any other.</summary>
    private const int MapVersion = 2;

    /// <summary>How a map's edges name the source root.</summary>
    private const string SourceModule = "source";

    /// <summary>
    /// The output names this root's plan consumes from the other side of the transfer. Empty when
    /// it consumes nothing, which is the usual case and what lets both roots run at once.
    /// </summary>
    public static List<string> NeedsOutputs(string? rootDirectory)
    {
        var root = string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory;
        var path = Path.Combine(root, TransferFiles.MapFile);

        if (!File.Exists(path)) return [];

        var map = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<TransferMapFile>(File.ReadAllText(path));

        if (map == null) return [];

        var moduleName = ModuleNameOf(map, root);
        if (moduleName == null) return [];

        return map.CrossEdges?
            .Where(e => e.Consumer == moduleName && e.Producer != moduleName && e.Output != null)
            .Select(e => e.Output!)
            .Distinct()
            .ToList() ?? [];
    }

    /// <summary>
    /// Which part this root plays and who the other party is, read from the map before anything
    /// runs. A receiver's map step fails outright without the source's fragment, so the role has to
    /// be known before the step rather than discovered by attempting it.
    /// </summary>
    public static TransferRole RoleOf(string? rootDirectory)
    {
        var root = string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory;
        var path = Path.Combine(root, TransferFiles.MapFile);

        if (!File.Exists(path))
            return new TransferRole { Kind = TransferRoleKind.Unknown, Problem = "this root has no transfer map" };

        var map = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<TransferMapFile>(File.ReadAllText(path));

        // An older map parses into this shape without complaint, keeping what matches and leaving
        // the rest empty, so the version is checked rather than the fields it would have filled.
        if (map is null || map.Version != MapVersion)
            return new TransferRole
            {
                Kind = TransferRoleKind.Unknown,
                Problem = $"the transfer map is version {map?.Version ?? 0} and this runner reads "
                          + $"version {MapVersion}; re-run the refactor at the source to regenerate it"
            };

        if (map.SourceDir == null || map.ReceiverDir == null)
            return new TransferRole { Kind = TransferRoleKind.Unknown, Problem = "the map does not name both of its roots" };

        var receiverBase = Path.GetFileName(Path.TrimEndingDirectorySeparator(map.ReceiverDir));
        var baseName = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)));

        if (baseName == map.SourceDir)
            return new TransferRole { Kind = TransferRoleKind.Source };

        if (baseName == receiverBase)
            return new TransferRole { Kind = TransferRoleKind.Receiver };

        return new TransferRole
        {
            Kind = TransferRoleKind.Unknown,
            Problem = $"the map names '{map.SourceDir}' and '{receiverBase}', but this root is '{baseName}'"
        };
    }

    /// <summary>
    /// How the map names this root in its cross edges. Roots are matched by directory name, which
    /// the map keeps unique; the source goes by the remainder's name and a receiver by its key.
    /// </summary>
    private static string? ModuleNameOf(TransferMapFile map, string root)
    {
        var baseName = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)));

        if (baseName == map.SourceDir) return SourceModule;

        return map.ReceiverDir is { } dir
               && Path.GetFileName(Path.TrimEndingDirectorySeparator(dir)) == baseName
            ? dir
            : null;
    }

    private class TransferMapFile
    {
        /// <summary>The format this map was written in. A map of another version is refused.</summary>
        public int Version { get; set; }

        public List<CrossEdge>? CrossEdges { get; set; }

        /// <summary>The source root's directory name.</summary>
        public string? SourceDir { get; set; }

        /// <summary>The receiving root's path, as the source names it and as the edges refer to it.</summary>
        public string? ReceiverDir { get; set; }
    }

    /// <summary>One value another Module produces and this one consumes.</summary>
    private class CrossEdge
    {
        public string? Consumer { get; set; }
        public string? Producer { get; set; }
        public string? Output { get; set; }
    }
}
