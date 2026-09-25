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
/// The steps a state list runs before it can list: pick the runner, fetch the code, initialise
/// the backend. Each job kind names its own, so a reply can be routed back to the saga that
/// asked for it.
/// </summary>
public class StateListFilteredSelectRunnerInstanceRequested : ManualStepRequestBase;

public class StateListFilteredSelectRunnerInstanceCompleted : ManualStepResponseBase
{
    /// <summary>The instance the job is pinned to for the rest of its steps.</summary>
    public string RunnerInstanceName { get; set; } = null!;
}

public class StateListFilteredSelectRunnerInstanceFaulted : ManualStepFaultedBase;

public class StateListFilteredGetModuleRequested : ManualStepRequestBase
{
    /// <summary>The ref to check out, where the job runs against one other than the Module's own.</summary>
    public string? SourceRevisionOverride { get; set; }
}

public class StateListFilteredGetModuleCompleted : ManualStepResponseBase
{
    public string? DefinitiveRevision { get; set; }
}

public class StateListFilteredGetModuleFaulted : ManualStepFaultedBase;

public class StateListFilteredInitRequested : ManualStepRequestBase;

public class StateListFilteredInitCompleted : ManualStepResponseBase;

public class StateListFilteredInitFaulted : ManualStepFaultedBase;
