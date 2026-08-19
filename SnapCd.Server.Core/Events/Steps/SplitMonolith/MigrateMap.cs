// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.SplitMonolith;

public class MigrateMapRequested : StepRequestBase
{
    /// <summary>Monolith root within the checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>Engine the migration runs with (--engine), or an explicit binary (--exec-path).</summary>
    public string? Engine { get; set; }
    public string? ExecPath { get; set; }
}

/// <summary>
/// This step performs an action rather than asserting one, so it either succeeds or faults.
/// A non-zero exit is a fault: a half-completed push is not a tidy outcome to report.
/// </summary>
public class MigrateMapCompleted : StepResponseBase
{
    /// <summary>Hash of the refactor map this carve was made against.</summary>
    public string? RefactorMapHash { get; set; }

    /// <summary>Names of the modules the carve produced. Receipts stay on the runner; this is the shape only.</summary>
    public List<string> CarvedModuleNames { get; set; } = [];

    public int ResourcesMoved { get; set; }
}

public class MigrateMapFaulted : StepFaultedBase;

public class MigrateMapCancelled : StepResponseBase;
