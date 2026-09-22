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

    /// <summary>
    /// The fragment the source writes is read back by name, which is how it reaches the receiver
    /// through the server: the two runners never see each other.
    /// </summary>
    [Fact]
    public async Task A_Fragment_Round_Trips_Through_The_Work_Directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            await TransferFiles.WriteFragment(root, "network", "{\"serial\":9}", "map_hash: abc");

            var (state, meta) = await TransferFiles.ReadFragment(root, "network");

            Assert.Equal("{\"serial\":9}", state);
            Assert.Equal("map_hash: abc", meta);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Reading_A_Fragment_That_Was_Never_Written_Gives_Nothing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        var (state, meta) = await TransferFiles.ReadFragment(root, "network");

        Assert.Null(state);
        Assert.Null(meta);
    }

    /// <summary>
    /// The values one Module's plan produced, picked up by filename so the server can hand them to
    /// whichever Module consumes them without knowing what is inside.
    /// </summary>
    [Fact]
    public async Task Output_Values_Round_Trip_By_Filename()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            await TransferFiles.WriteOutputs(root, new Dictionary<string, string>
            {
                ["outputs-app.yaml"] = "db_endpoint: db.example.com"
            });

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
    [Fact]
    public async Task The_Map_Says_Which_Module_Needs_Values_From_The_Other()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            await TransferFiles.WriteMap(root, """
                                               version: 1
                                               cross_edges:
                                                 - consumer: app
                                                   input: db_endpoint
                                                   producer: network
                                                   output: endpoint
                                               """);

            Assert.Equal(["network"], TransferMap.NeedsValuesFrom(root, "app"));
            Assert.Empty(TransferMap.NeedsValuesFrom(root, "network"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void A_Map_With_No_Wiring_Means_Neither_Module_Waits()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");

        Assert.Empty(TransferMap.NeedsValuesFrom(root, "app"));
    }
}
