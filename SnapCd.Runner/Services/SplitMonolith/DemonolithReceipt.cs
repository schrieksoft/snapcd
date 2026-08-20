// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SnapCd.Runner.Services.SplitMonolith;

/// <summary>
/// The parts of a demonolith receipt the server is told about. The receipt itself stays on the
/// runner; this is deliberately a subset.
/// </summary>
public class DemonolithReceipt
{
    public const string MapReceiptFile = "demonolith-migrate-map.yaml";
    public const string RunReceiptFile = "demonolith-migrate-run.yaml";
    public const string ProveReceiptFile = "demonolith-migrate-prove.yaml";
    public const string VerifyReceiptFile = "demonolith-migrate-verify.yaml";

    /// <summary>Receipt schema version. Demonolith refuses a receipt newer than it understands.</summary>
    public int Version { get; set; }

    /// <summary>Checksum of the map this action was made against.</summary>
    [YamlMember(Alias = "map_checksum")]
    public string? MapChecksum { get; set; }

    /// <summary>Whether the action ran to completion.</summary>
    public bool Complete { get; set; }

    /// <summary>Each carved module and the state the action left it in.</summary>
    [YamlMember(Alias = "module_states")]
    public Dictionary<string, string> ModuleStates { get; set; } = [];

    /// <summary>
    /// Reads a receipt from beside the monolith root, or null when it is absent — a step that did
    /// not get far enough to write one.
    /// </summary>
    public static DemonolithReceipt? Read(string? rootDirectory, string receiptFile)
    {
        var path = Path.Combine(
            string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory,
            receiptFile);

        if (!File.Exists(path)) return null;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<DemonolithReceipt>(File.ReadAllText(path));
    }
}
