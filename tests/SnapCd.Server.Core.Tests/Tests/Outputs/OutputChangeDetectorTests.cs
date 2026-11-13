// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Services.Utils.Outputs;

namespace SnapCd.Server.Core.Tests.Tests.Outputs;

/// <summary>
/// Which outputs are reported as created or updated. A blank sensitive value has no vault write,
/// so its change cannot be read from the vault's answer.
/// </summary>
public class OutputChangeDetectorTests
{
    private const string Name = "db_password";

    private static OutputSet Set(params Output[] outputs) =>
        new() { Id = Guid.NewGuid(), Outputs = outputs.ToList() };

    private static SecretOutput Secret(bool blank) =>
        new() { Name = Name, Type = "string", RemoteSecretName = $"{Name}-secret", BlankOutput = blank };

    private static LiteralOutput Literal(string value) =>
        new() { Name = Name, Type = "string", Value = value };

    [Fact]
    public void A_Value_Becoming_Blank_Is_A_Change()
    {
        var changed = OutputChangeDetector.DetectChanges(
            Set(Secret(blank: false)), Set(Secret(blank: true)), new Dictionary<string, bool>());

        Assert.Equal([Name], changed);
    }

    [Fact]
    public void A_Blank_Value_Becoming_Set_Is_A_Change()
    {
        var changed = OutputChangeDetector.DetectChanges(
            Set(Secret(blank: true)), Set(Secret(blank: false)), new Dictionary<string, bool> { [Name] = true });

        Assert.Equal([Name], changed);
    }

    /// <summary>
    /// Two applies that are both blank write nothing to the vault, so there is nothing to report.
    /// </summary>
    [Fact]
    public void A_Value_That_Stays_Blank_Is_Not_A_Change()
    {
        var changed = OutputChangeDetector.DetectChanges(
            Set(Secret(blank: true)), Set(Secret(blank: true)), new Dictionary<string, bool>());

        Assert.Empty(changed);
    }

    [Fact]
    public void A_Non_Blank_Value_Still_Reports_What_The_Vault_Said()
    {
        var unchanged = OutputChangeDetector.DetectChanges(
            Set(Secret(blank: false)), Set(Secret(blank: false)), new Dictionary<string, bool> { [Name] = false });

        Assert.Empty(unchanged);

        var changed = OutputChangeDetector.DetectChanges(
            Set(Secret(blank: false)), Set(Secret(blank: false)), new Dictionary<string, bool> { [Name] = true });

        Assert.Equal([Name], changed);
    }

    [Fact]
    public void An_Output_Changing_Class_Is_A_Change()
    {
        var toSecret = OutputChangeDetector.DetectChanges(
            Set(Literal("abc")), Set(Secret(blank: true)), new Dictionary<string, bool>());

        Assert.Equal([Name], toSecret);
    }

    [Fact]
    public void A_First_Blank_Output_Is_A_Change()
    {
        var changed = OutputChangeDetector.DetectChanges(
            previous: null, Set(Secret(blank: true)), new Dictionary<string, bool>());

        Assert.Equal([Name], changed);
    }
}
