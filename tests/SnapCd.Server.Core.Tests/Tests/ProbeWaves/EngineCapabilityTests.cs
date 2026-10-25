// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What each engine declares it can be asked to do. A probe that reaches a Module whose engine
/// answers false is resolved as unanswerable and releases whatever waited on it, so these answers
/// decide whether a wave propagates or stops at that Module.
/// </summary>
public class EngineCapabilityTests
{
    [Theory]
    [InlineData(StateManagementEngine.OpenTofu, true)]
    [InlineData(StateManagementEngine.Terraform, true)]
    [InlineData(StateManagementEngine.Pulumi, false)]
    public void Engines_Declare_Whether_They_Can_Be_Probed(StateManagementEngine engine, bool expected)
    {
        Assert.Equal(expected, engine.SupportsProbes());
    }

    [Theory]
    [InlineData(StateManagementEngine.OpenTofu, true)]
    [InlineData(StateManagementEngine.Terraform, true)]
    [InlineData(StateManagementEngine.Pulumi, false)]
    public void Engines_Declare_Whether_They_Support_State_Migrations(
        StateManagementEngine engine, bool expected)
    {
        Assert.Equal(expected, engine.SupportsStateMigrations());
    }

    /// <summary>
    /// A new engine answers both questions explicitly. The switches fall through to false, so an
    /// engine added without an arm silently loses probes and state migrations.
    /// </summary>
    [Fact]
    public void Every_Engine_Is_Answered_Explicitly()
    {
        var unanswered = Enum.GetValues<StateManagementEngine>()
            .Where(e => !e.SupportsProbes() && !e.SupportsStateMigrations())
            .ToList();

        Assert.Equal([StateManagementEngine.Pulumi], unanswered);
    }
}
