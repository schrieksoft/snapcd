// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.ManualJobs;

/// <summary>
/// The steps every manual job runs before its own work: pick the runner instance the job is pinned
/// to, check out the code, initialise the backend.
///
/// The runner does one piece of work per step - one checkout, one init - but each family has its own
/// messages either way. A dispatch queue is named after its message type, so a shared request would
/// put four families on one queue; and a reply has to find one saga.
/// </summary>
public abstract class ManualGetModuleRequestedBase : ManualStepRequestBase
{
    /// <summary>The ref to check out, where the job runs against one other than the Module's own.</summary>
    public string? SourceRevisionOverride { get; set; }
}

public class StateListFilteredSelectRunnerInstanceRequested : ManualStepRequestBase;
public class StateListFilteredGetModuleRequested : ManualGetModuleRequestedBase;
public class StateListFilteredInitRequested : ManualStepRequestBase;

public class MoveSelectRunnerInstanceRequested : ManualStepRequestBase;
public class MoveGetModuleRequested : ManualGetModuleRequestedBase;
public class MoveInitRequested : ManualStepRequestBase;

public class ImportSelectRunnerInstanceRequested : ManualStepRequestBase;
public class ImportGetModuleRequested : ManualGetModuleRequestedBase;
public class ImportInitRequested : ManualStepRequestBase;

public class RemoveSelectRunnerInstanceRequested : ManualStepRequestBase;
public class RemoveGetModuleRequested : ManualGetModuleRequestedBase;
public class RemoveInitRequested : ManualStepRequestBase;

/// <summary>A reply naming the instance the job is now pinned to.</summary>
public abstract class ManualSelectRunnerInstanceCompletedBase : ManualStepResponseBase
{
    public string RunnerInstanceName { get; set; } = null!;
}

/// <summary>A reply naming the ref the checkout resolved to.</summary>
public abstract class ManualGetModuleCompletedBase : ManualStepResponseBase
{
    public string? DefinitiveRevision { get; set; }
}

public class StateListFilteredSelectRunnerInstanceCompleted : ManualSelectRunnerInstanceCompletedBase;
public class StateListFilteredSelectRunnerInstanceCancelled : ManualStepResponseBase;
public class StateListFilteredSelectRunnerInstanceFaulted : ManualStepFaultedBase;
public class StateListFilteredGetModuleCompleted : ManualGetModuleCompletedBase;
public class StateListFilteredGetModuleCancelled : ManualStepResponseBase;
public class StateListFilteredGetModuleFaulted : ManualStepFaultedBase;
public class StateListFilteredInitCompleted : ManualStepResponseBase;
public class StateListFilteredInitCancelled : ManualStepResponseBase;
public class StateListFilteredInitFaulted : ManualStepFaultedBase;

public class MoveSelectRunnerInstanceCompleted : ManualSelectRunnerInstanceCompletedBase;
public class MoveSelectRunnerInstanceCancelled : ManualStepResponseBase;
public class MoveSelectRunnerInstanceFaulted : ManualStepFaultedBase;
public class MoveGetModuleCompleted : ManualGetModuleCompletedBase;
public class MoveGetModuleCancelled : ManualStepResponseBase;
public class MoveGetModuleFaulted : ManualStepFaultedBase;
public class MoveInitCompleted : ManualStepResponseBase;
public class MoveInitCancelled : ManualStepResponseBase;
public class MoveInitFaulted : ManualStepFaultedBase;

public class ImportSelectRunnerInstanceCompleted : ManualSelectRunnerInstanceCompletedBase;
public class ImportSelectRunnerInstanceCancelled : ManualStepResponseBase;
public class ImportSelectRunnerInstanceFaulted : ManualStepFaultedBase;
public class ImportGetModuleCompleted : ManualGetModuleCompletedBase;
public class ImportGetModuleCancelled : ManualStepResponseBase;
public class ImportGetModuleFaulted : ManualStepFaultedBase;
public class ImportInitCompleted : ManualStepResponseBase;
public class ImportInitCancelled : ManualStepResponseBase;
public class ImportInitFaulted : ManualStepFaultedBase;

public class RemoveSelectRunnerInstanceCompleted : ManualSelectRunnerInstanceCompletedBase;
public class RemoveSelectRunnerInstanceCancelled : ManualStepResponseBase;
public class RemoveSelectRunnerInstanceFaulted : ManualStepFaultedBase;
public class RemoveGetModuleCompleted : ManualGetModuleCompletedBase;
public class RemoveGetModuleCancelled : ManualStepResponseBase;
public class RemoveGetModuleFaulted : ManualStepFaultedBase;
public class RemoveInitCompleted : ManualStepResponseBase;
public class RemoveInitCancelled : ManualStepResponseBase;
public class RemoveInitFaulted : ManualStepFaultedBase;
