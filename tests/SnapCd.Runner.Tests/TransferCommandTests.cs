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
    /// The fragment the source writes is read back from the work directory, which is how it
    /// reaches the receiver through the server: the two runners never see each other. demonolith
    /// names it after the receiving root, so it is found rather than named.
    /// </summary>
    [Fact]
    public async Task A_Fragment_Round_Trips_Through_The_Work_Directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        try
        {
            await TransferFiles.WriteFragment(root, "{\"serial\":9}", "map_hash: abc");

            var (state, meta) = await TransferFiles.ReadFragment(root);

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
        var (state, meta) = await TransferFiles.ReadFragment(root);

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
    /// <summary>
    /// The map names each root by its own directory, so which values a root needs is worked out
    /// from where it is checked out rather than from anything the server passes.
    /// </summary>
    [Fact]
    public async Task A_Receiving_Root_Needs_The_Values_The_Map_Says_It_Consumes()
    {
        var work = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        var root = Path.Combine(work, "app");
        try
        {
            Directory.CreateDirectory(root);
            await TransferFiles.WriteMap(root, Map);

            // The producer is how the map names the source: the remainder, not its directory.
            Assert.Equal(["legacy"], TransferMap.NeedsValuesFrom(root));
        }
        finally
        {
            if (Directory.Exists(work)) Directory.Delete(work, true);
        }
    }

    /// <summary>The source goes by the remainder's name, which is what its edges are keyed by.</summary>
    [Fact]
    public async Task The_Source_Root_Needs_Nothing_When_Only_The_Receiver_Consumes()
    {
        var work = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");
        var root = Path.Combine(work, "network");
        try
        {
            Directory.CreateDirectory(root);
            await TransferFiles.WriteMap(root, Map);

            Assert.Empty(TransferMap.NeedsValuesFrom(root));
        }
        finally
        {
            if (Directory.Exists(work)) Directory.Delete(work, true);
        }
    }

    [Fact]
    public void A_Map_With_No_Wiring_Means_Neither_Module_Waits()
    {
        var root = Path.Combine(Path.GetTempPath(), $"transfer-{Guid.NewGuid():N}");

        Assert.Empty(TransferMap.NeedsValuesFrom(root));
    }

    /// <summary>
    /// A map wiring one value across: the receiving root "app" consumes what the source produces.
    /// The source root is "network", which the edges call by the remainder's name.
    /// </summary>
    private const string Map =
        """
        version: 1
        source_dir: network
        remainder: legacy
        receivers:
            app:
                moves:
                    - random_pet.move_me
        cross_edges:
            - consumer: app
              input: db_endpoint
              producer: legacy
              output: endpoint
        """;
}
