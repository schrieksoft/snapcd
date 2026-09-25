// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.ManualJobs;

namespace SnapCd.Server.Core.Events.Steps.StateMigrations;

/// <summary>
/// The steps a state edit runs before it edits: pick the runner, fetch the code, initialise the
/// backend. A move, an import and a remove share one saga, so they share these too; the operation
/// the job was asked for travels on the edit's own request.
/// </summary>
public class StateMoveSelectRunnerInstanceRequested : ManualStepRequestBase;

public class StateMoveSelectRunnerInstanceCompleted : ManualStepResponseBase
{
    /// <summary>The instance the job is pinned to for the rest of its steps.</summary>
    public string RunnerInstanceName { get; set; } = null!;
}

public class StateMoveSelectRunnerInstanceFaulted : ManualStepFaultedBase;

public class StateMoveGetModuleRequested : ManualStepRequestBase
{
    /// <summary>The ref to check out, where the job runs against one other than the Module's own.</summary>
    public string? SourceRevisionOverride { get; set; }
}

public class StateMoveGetModuleCompleted : ManualStepResponseBase
{
    public string? DefinitiveRevision { get; set; }
}

public class StateMoveGetModuleFaulted : ManualStepFaultedBase;

public class StateMoveInitRequested : ManualStepRequestBase;

public class StateMoveInitCompleted : ManualStepResponseBase;

public class StateMoveInitFaulted : ManualStepFaultedBase;
