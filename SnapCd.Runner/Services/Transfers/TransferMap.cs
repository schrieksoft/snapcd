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

        if (map?.SourceDir == null)
            return new TransferRole { Kind = TransferRoleKind.Unknown, Problem = "the map does not name its source root" };

        var receivers = map.Receivers?.Keys.ToList() ?? [];

        // A transfer moves resources between two Modules. The map format still carries several
        // receivers from an older design, and taking the first would move state nobody asked about.
        if (receivers.Count != 1)
            return new TransferRole
            {
                Kind = TransferRoleKind.Unknown,
                Problem = $"a transfer moves resources to one module; this map names {receivers.Count}"
            };

        var receiverBase = Path.GetFileName(Path.TrimEndingDirectorySeparator(receivers[0]));
        var baseName = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)));

        if (baseName == map.SourceDir)
            return new TransferRole { Kind = TransferRoleKind.Source, ReceiverBase = receiverBase };

        if (baseName == receiverBase)
            return new TransferRole { Kind = TransferRoleKind.Receiver, ReceiverBase = receiverBase };

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

        if (baseName == map.SourceDir) return map.Remainder;

        return map.Receivers?.Keys.FirstOrDefault(key =>
            Path.GetFileName(Path.TrimEndingDirectorySeparator(key)) == baseName);
    }

    private class TransferMapFile
    {
        public List<CrossEdge>? CrossEdges { get; set; }

        /// <summary>The source root's directory name.</summary>
        public string? SourceDir { get; set; }

        /// <summary>What the edges call the source: the part of it that stays behind.</summary>
        public string? Remainder { get; set; }

        /// <summary>Keyed by each receiving root's path as the map spells it.</summary>
        public Dictionary<string, object>? Receivers { get; set; }
    }

    /// <summary>One value another Module produces and this one consumes.</summary>
    private class CrossEdge
    {
        public string? Consumer { get; set; }
        public string? Producer { get; set; }
        public string? Output { get; set; }
    }
}
