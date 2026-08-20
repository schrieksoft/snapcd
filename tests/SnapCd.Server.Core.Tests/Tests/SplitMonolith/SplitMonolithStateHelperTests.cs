// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Helpers.SplitMonolith;
using SnapCd.Server.Core.StateMachine.SplitMonolith;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.SplitMonolith;

/// <summary>
/// The state map is consulted during runner authorization. A missing entry throws there, which is
/// both late and hard to read, so the mapping is asserted to be total here instead.
/// </summary>
public class SplitMonolithStateHelperTests
{
    [Fact]
    public void Every_Callback_Maps_To_A_State()
    {
        var unmapped = new List<SplitMonolithTaskEndpoint>();

        foreach (var endpoint in Enum.GetValues<SplitMonolithTaskEndpoint>())
        {
            try
            {
                SplitMonolithStateHelper.Lookup(endpoint);
            }
            catch (ArgumentOutOfRangeException)
            {
                unmapped.Add(endpoint);
            }
        }

        Assert.True(unmapped.Count == 0,
            $"No saga state is mapped for: {string.Join(", ", unmapped)}");
    }

    /// <summary>
    /// A callback is only accepted while the saga sits in the state that dispatched it, so every
    /// mapped state must be one the machine actually declares.
    /// </summary>
    [Fact]
    public void Every_Mapped_State_Exists_On_The_Machine()
    {
        var machine = new SplitMonolithStateMachine(NullLogger<SplitMonolithStateMachine>.Instance);
        var declared = machine.States.Select(s => s.Name).ToHashSet();

        var missing = Enum.GetValues<SplitMonolithTaskEndpoint>()
            .Select(SplitMonolithStateHelper.Lookup)
            .Select(s => s.ToString())
            .Distinct()
            .Where(s => !declared.Contains(s))
            .ToList();

        Assert.True(missing.Count == 0,
            $"Mapped to states the machine does not declare: {string.Join(", ", missing)}");
    }

    /// <summary>The cancelling states must be real states too, or a mid-cancellation callback is refused.</summary>
    [Fact]
    public void Cancelling_States_Exist_On_The_Machine()
    {
        var machine = new SplitMonolithStateMachine(NullLogger<SplitMonolithStateMachine>.Instance);
        var declared = machine.States.Select(s => s.Name).ToHashSet();

        var missing = SplitMonolithStateHelper.GetCancellingStates()
            .Select(s => s.ToString())
            .Where(s => !declared.Contains(s))
            .ToList();

        Assert.True(missing.Count == 0,
            $"Cancelling states the machine does not declare: {string.Join(", ", missing)}");
    }
}
