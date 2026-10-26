// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Views;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// Whether a prediction still holds. A probe plans against the code, the declaration and the
/// resolved input values of one moment, so it answers for that moment and no other.
/// </summary>
public class JobFingerprintTests
{
    private static JobFingerprint Print(
        string? revision = "r1",
        string? declaration = "d1",
        string? parameters = "p1",
        string? envVars = "e1") =>
        new(ExecutionStatus.Completed, revision, declaration, parameters, envVars);

    [Fact]
    public void Nothing_Moved_Is_Not_Stale()
    {
        Assert.Empty(Print().Against(Print()));
    }

    [Fact]
    public void A_New_Commit_Is_Stale()
    {
        Assert.Equal([StalenessReason.Revision], Print().Against(Print(revision: "r2")));
    }

    [Fact]
    public void A_Changed_Declaration_Is_Stale()
    {
        Assert.Equal([StalenessReason.Declaration], Print().Against(Print(declaration: "d2")));
    }

    /// <summary>
    /// The case a revision comparison cannot see: nobody edited anything, but an upstream's output
    /// moved, so the values this Module resolves are no longer the ones it was probed with.
    /// </summary>
    [Fact]
    public void A_Changed_Input_Value_Is_Stale()
    {
        Assert.Equal([StalenessReason.Parameters], Print().Against(Print(parameters: "p2")));
    }

    [Fact]
    public void A_Changed_Environment_Variable_Is_Stale()
    {
        Assert.Equal([StalenessReason.EnvironmentVariables], Print().Against(Print(envVars: "e2")));
    }

    [Fact]
    public void Several_Can_Move_At_Once()
    {
        var moved = Print().Against(Print(revision: "r2", parameters: "p2"));

        Assert.Equal([StalenessReason.Revision, StalenessReason.Parameters], moved);
    }

    /// <summary>
    /// A hash absent on either side says the comparison cannot be made, not that it failed. A job
    /// that ran before these were recorded has none, and reporting that as a change would mark
    /// every probe before the upgrade stale.
    /// </summary>
    [Fact]
    public void A_Missing_Hash_Is_Not_A_Difference()
    {
        Assert.Empty(Print(declaration: null).Against(Print(declaration: "d2")));
        Assert.Empty(Print().Against(Print(declaration: null)));
    }

    [Fact]
    public void Only_The_Parts_That_Moved_Are_Reported()
    {
        var moved = Print().Against(Print(envVars: "e2"));

        Assert.DoesNotContain(StalenessReason.Revision, moved);
        Assert.DoesNotContain(StalenessReason.Declaration, moved);
        Assert.DoesNotContain(StalenessReason.Parameters, moved);
    }
}
