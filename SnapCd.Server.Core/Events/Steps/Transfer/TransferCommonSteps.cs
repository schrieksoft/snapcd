// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Events.Steps.Transfer;

// The four steps a participant runs before any transfer slice: the same work a deployment does,
// but addressed to one side of the transfer and pinned to that side's runner.

/// <summary>Pins the runner instance this participant's steps will all go to.</summary>
public class TransferSelectRunnerInstanceRequested : TransferStepRequestBase;

/// <summary>Carries the instance the selection settled on, which the saga pins for the side.</summary>
public class TransferSelectRunnerInstanceCompleted : TransferStepResponseBase
{
    public string RunnerInstanceName { get; set; } = string.Empty;
}

public class TransferSelectRunnerInstanceFaulted : TransferStepFaultedBase;

/// <summary>Checks the participant's source out at the ref it consented to prove.</summary>
public class TransferGetModuleRequested : TransferStepRequestBase
{
    /// <summary>The ref from this participant's side of the Transfer, not the Module's own.</summary>
    public string? SourceRevisionOverride { get; set; }
}

public class TransferGetModuleCompleted : TransferStepResponseBase
{
    /// <summary>The commit the override resolved to, which the proof is recorded against.</summary>
    public string? DefinitiveRevision { get; set; }
}

public class TransferGetModuleFaulted : TransferStepFaultedBase;

public class TransferInitRequested : TransferStepRequestBase;

public class TransferInitCompleted : TransferStepResponseBase;

public class TransferInitFaulted : TransferStepFaultedBase;

public class TransferValidateRequested : TransferStepRequestBase;

public class TransferValidateCompleted : TransferStepResponseBase;

public class TransferValidateFaulted : TransferStepFaultedBase;

public class TransferPlanRequested : TransferStepRequestBase;

/// <summary>
/// A plan for one participant. A transfer proves against a clean plan, so anything but zero
/// changes is a red step for that side.
/// </summary>
public class TransferPlanCompleted : TransferStepResponseBase
{
    public int TotalChangedCount { get; set; }
}

public class TransferPlanFaulted : TransferStepFaultedBase;
