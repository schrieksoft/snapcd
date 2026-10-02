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
/// The parts of a demonolith transfer receipt the server is told about. Transfers write their own
/// receipts, separate from the split's.
/// </summary>
public class TransferReceipt
{
    public const string MapReceiptFile = "demonolith-transfer-migrate-map.yaml";
    public const string ProveReceiptFile = "demonolith-transfer-prove.yaml";
    public const string RunReceiptFile = "demonolith-transfer-run.yaml";
    public const string VerifyReceiptFile = "demonolith-transfer-verify.yaml";

    public int Version { get; set; }

    /// <summary>Hash of the map this step ran against.</summary>
    [YamlMember(Alias = "map_hash")]
    public string? MapHash { get; set; }

    /// <summary>The root's part in the transfer, as demonolith derived it from the map.</summary>
    public string? Role { get; set; }

    /// <summary>Whether the step ran to completion.</summary>
    public bool Ok { get; set; }

    /// <summary>The addresses this root's state gave up or took on.</summary>
    [YamlMember(Alias = "transferred_addresses")]
    public List<string> TransferredAddresses { get; set; } = [];

    /// <summary>Reads a receipt from the root, or null when the step wrote none.</summary>
    public static TransferReceipt? Read(string? rootDirectory, string receiptFile)
    {
        var path = Path.Combine(
            string.IsNullOrWhiteSpace(rootDirectory) ? "." : rootDirectory,
            receiptFile);

        if (!File.Exists(path)) return null;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<TransferReceipt>(File.ReadAllText(path));
    }
}
