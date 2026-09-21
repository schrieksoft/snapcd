// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Runner.Services.SplitMonolith;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The command line handed to demonolith. A root directory with a space must survive as one
/// argument, and flags Snap CD does not offer must never appear.
/// </summary>
public class DemonolithCommandTests
{
    [Fact]
    public void Builds_The_Bare_Subcommand()
    {
        Assert.Equal("demonolith split refactor diff", DemonolithCommand.Build("split refactor diff", null, null));
    }

    [Fact]
    public void Quotes_The_Root_Directory()
    {
        var command = DemonolithCommand.Build("split migrate map", "infra/my monolith", null);

        Assert.Contains("--root-dir \"infra/my monolith\"", command);
    }

    [Fact]
    public void Omits_An_Empty_Root_Directory_And_Engine()
    {
        var command = DemonolithCommand.Build("split migrate prove", "   ", "  ");

        Assert.Equal("demonolith split migrate prove", command);
    }

    [Fact]
    public void Lowercases_The_Engine()
    {
        Assert.Contains("--engine tofu", DemonolithCommand.Build("split migrate map", null, "Tofu"));
    }

    /// <summary>--exec-path lets a caller name a binary, and --yes belongs to the pipeline Snap CD never runs.</summary>
    [Fact]
    public void Never_Offers_An_Exec_Path_Or_A_Confirmation()
    {
        var command = DemonolithCommand.Build("split migrate run", "root", "tofu", "--overwrite");

        Assert.DoesNotContain("--exec-path", command);
        Assert.DoesNotContain("--yes", command);
        Assert.DoesNotContain("--output", command);
        Assert.Contains("--overwrite", command);
    }

    [Fact]
    public void Drops_Blank_Extra_Flags()
    {
        var command = DemonolithCommand.Build("split migrate run", null, null, "", "   ", "--overwrite");

        Assert.Equal("demonolith split migrate run --overwrite", command);
    }

    [Fact]
    public void Quotes_Each_Backend_Config()
    {
        var flags = DemonolithCommand.BackendConfigFlags(["key=value", "  ", "other=thing"]).ToList();

        Assert.Equal(["--backend-config \"key=value\"", "--backend-config \"other=thing\""], flags);
    }
}
