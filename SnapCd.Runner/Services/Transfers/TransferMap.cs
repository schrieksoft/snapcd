// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SnapCd.Runner.Services.Transfers;

/// <summary>
/// Reads the transfer map for the one thing the server cannot work out for itself: which Module
/// needs a value the other one produces. A Module that needs a value cannot plan until the other
/// has, so this is what decides which of the two goes first.
/// </summary>
public static class TransferMap
{
    /// <summary>
    /// Module names this one needs values from. Empty when it needs nothing, which is what lets
    /// the two run in either order.
    /// </summary>
    public static List<string> NeedsValuesFrom(string? rootDirectory, string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName)) return [];

        var path = Path.Combine(
            string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory,
            TransferFiles.MapFile);

        if (!File.Exists(path)) return [];

        var map = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<TransferMapFile>(File.ReadAllText(path));

        return map?.CrossEdges?
            .Where(e => e.Consumer == moduleName && e.Producer != null)
            .Select(e => e.Producer!)
            .Distinct()
            .ToList() ?? [];
    }

    private class TransferMapFile
    {
        public List<CrossEdge>? CrossEdges { get; set; }
    }

    /// <summary>One value another Module produces and this one consumes.</summary>
    private class CrossEdge
    {
        public string? Consumer { get; set; }
        public string? Producer { get; set; }
    }
}
