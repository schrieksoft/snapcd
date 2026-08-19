// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.SplitMonolith;

public class MigrateProveRequested : StepRequestBase
{
    /// <summary>Monolith root within the checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>Engine the migration runs with (--engine), or an explicit binary (--exec-path).</summary>
    public string? Engine { get; set; }
    public string? ExecPath { get; set; }
}

/// <summary>
/// Demonolith answers three ways: 0 success, 1 operational error, 2 a negative verdict — the run
/// worked and the answer is no. This step is an assertion, so a verdict is a legitimate outcome
/// carried here rather than as a fault, and the job stops cleanly showing what failed the check.
/// </summary>
public class MigrateProveCompleted : StepResponseBase
{
    public bool NegativeVerdict { get; set; }
    public string? VerdictReason { get; set; }

    public int ModulesProven { get; set; }
    public int ModulesPlanningClean { get; set; }
}

public class MigrateProveFaulted : StepFaultedBase;

public class MigrateProveCancelled : StepResponseBase;
