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

/// <summary>One address to act on, and where to. A remove names only the address.</summary>
public class AddressInstruction
{
    public string Address { get; set; } = null!;

    public string? Target { get; set; }
}

/// <summary>
/// Asks the runner to move, import or remove a batch of addresses. Each runs on its own, so the
/// reply says which of them worked rather than whether the batch did.
/// </summary>
public class StateMoveRequested : StepRequestBase
{
    public AddressOperation Operation { get; set; }

    public List<AddressInstruction> Instructions { get; set; } = [];
}

/// <summary>What the batch managed, address by address.</summary>
public class StateMoveCompleted : StepResponseBase
{
    public AddressOperation Operation { get; set; }

    public List<AddressResult> Results { get; set; } = [];
}

public class StateMoveFaulted : StepFaultedBase;
