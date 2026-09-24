// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.StateMigrations;

/// <summary>Asks the runner which of these addresses are in the Module's state.</summary>
public class StateListFilteredRequested : StepRequestBase
{
    public List<string> Addresses { get; set; } = [];
}

/// <summary>What the runner found, one entry per address asked about and no others.</summary>
public class StateListFilteredCompleted : StepResponseBase
{
    public List<AddressResult> Results { get; set; } = [];
}

public class StateListFilteredFaulted : StepFaultedBase;

/// <summary>One address and what became of it.</summary>
public class AddressResult
{
    public string Address { get; set; } = null!;

    /// <summary>An mv's destination or an import's resource id, where the operation has one.</summary>
    public string? Target { get; set; }

    public AddressOutcome Outcome { get; set; }
}
