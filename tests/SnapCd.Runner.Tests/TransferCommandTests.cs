// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Runner.Services.SplitMigrate;
using SnapCd.Runner.Services.Transfers;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The command line is the contract with demonolith, and it is checked here against the flags the
/// tool actually accepts: --root-dir and --engine, and nothing else. Backend settings in
/// particular are never passed, because each root's own backend file already holds them and an
/// override would point every root at the Module the transfer started from.
/// </summary>
public class TransferCommandTests
{
    [Theory]
    [InlineData("transfer migrate map")]
    [InlineData("transfer migrate prove")]
    [InlineData("transfer refactor diff")]
    public void A_Transfer_Command_Passes_Only_The_Root_And_The_Engine(string subcommand)
    {
        var command = DemonolithCommand.Build(subcommand, "roots/app", "tofu");

        Assert.Equal($"demonolith {subcommand} --root-dir \"roots/app\" --engine tofu", command);
    }

    /// <summary>
    /// Snap CD runs both Modules at once and tracks what moved, so demonolith's ordering interlock
    /// is waived. Without this the source would be refused until the receiver's receipt had been
    /// carried into its workdir, which is the sequencing Snap CD does not do.
    /// </summary>
    [Fact]
    public void The_Write_Waives_Demonoliths_Receipt_Interlock()
    {
        var command = DemonolithCommand.Build(
            "transfer migrate run", "roots/app", "tofu", "--no-receipt-check");

        Assert.Equal(
            "demonolith transfer migrate run --root-dir \"roots/app\" --engine tofu --no-receipt-check",
            command);
    }

    [Fact]
    public void A_Transfer_Command_Never_Carries_Backend_Settings()
    {
        var command = DemonolithCommand.Build("transfer migrate map", "roots/app", "tofu");

        Assert.DoesNotContain("--backend-config", command);
    }

    /// <summary>The map is the transfer's identity, so it is written byte for byte.</summary>
    [Fact]
    public async Task The_Map_Is_Written_Exactly_As_Given()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            const string map = "version: 1\nreceivers:\n  network: {}\n";
            await TransferFiles.WriteMap(root, map);

            Assert.Equal(map, await File.ReadAllTextAsync(Path.Combine(root, TransferFiles.MapFile)));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    /// <summary>The outputs a plan wrote are read back by the filename demonolith gave them.</summary>
    [Fact]
    public async Task Output_Values_Are_Read_Back_By_Filename()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            var work = Path.Combine(root, ".demono-transfer");
            Directory.CreateDirectory(work);
            await File.WriteAllTextAsync(
                Path.Combine(work, "outputs-app.yaml"), "db_endpoint: db.example.com");

            var outputs = await TransferFiles.ReadOutputs(root);

            var only = Assert.Single(outputs);
            Assert.Equal("outputs-app.yaml", only.Key);
            Assert.Equal("db_endpoint: db.example.com", only.Value);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// Which Module needs a value from which, read out of the map. This is what decides the order
    /// the two Modules run in.
    /// </summary>
    /// <summary>
    /// The map names each root by its own directory, so which values a root needs is worked out
    /// from where it is checked out rather than from anything the server passes.
    /// </summary>
}
