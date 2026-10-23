// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Events.Steps.StateMigrations;

/// <summary>
/// The steps every manual job runs before its own work: pick the runner instance the job is pinned
/// to, check out the code, initialise the backend.
///
/// The runner does one piece of work per step - one checkout, one init - but each family has its own
/// messages either way. A dispatch queue is named after its message type, so a shared request would
/// put four families on one queue; and a reply has to find one saga.
/// </summary>
public abstract class StateMigrationGetModuleRequestedBase : StateMigrationStepRequestBase
{
    /// <summary>The ref to check out, where the job runs against one other than the Module's own.</summary>
    public string? SourceRevisionOverride { get; set; }
}

public class LookupAddressesSelectRunnerInstanceRequested : StateMigrationStepRequestBase;
public class LookupAddressesGetModuleRequested : StateMigrationGetModuleRequestedBase;
public class LookupAddressesInitRequested : StateMigrationStepRequestBase;

public class MoveSelectRunnerInstanceRequested : StateMigrationStepRequestBase;
public class MoveGetModuleRequested : StateMigrationGetModuleRequestedBase;
public class MoveInitRequested : StateMigrationStepRequestBase;

public class ImportSelectRunnerInstanceRequested : StateMigrationStepRequestBase;
public class ImportGetModuleRequested : StateMigrationGetModuleRequestedBase;
public class ImportInitRequested : StateMigrationStepRequestBase;

public class RemoveSelectRunnerInstanceRequested : StateMigrationStepRequestBase;
public class RemoveGetModuleRequested : StateMigrationGetModuleRequestedBase;
public class RemoveInitRequested : StateMigrationStepRequestBase;

/// <summary>A reply naming the instance the job is now pinned to.</summary>
public abstract class StateMigrationSelectRunnerInstanceCompletedBase : StateMigrationStepResponseBase
{
    public string RunnerInstanceName { get; set; } = null!;
}

/// <summary>A reply naming the ref the checkout resolved to.</summary>
public abstract class StateMigrationGetModuleCompletedBase : StateMigrationStepResponseBase
{
    public string? DefinitiveRevision { get; set; }
}

public class LookupAddressesSelectRunnerInstanceCompleted : StateMigrationSelectRunnerInstanceCompletedBase;
public class LookupAddressesSelectRunnerInstanceCancelled : StateMigrationStepResponseBase;
public class LookupAddressesSelectRunnerInstanceFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.SelectRunnerInstance;
}
public class LookupAddressesGetModuleCompleted : StateMigrationGetModuleCompletedBase;
public class LookupAddressesGetModuleCancelled : StateMigrationStepResponseBase;
public class LookupAddressesGetModuleFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.GetModule;
}
public class LookupAddressesInitCompleted : StateMigrationStepResponseBase;
public class LookupAddressesInitCancelled : StateMigrationStepResponseBase;
public class LookupAddressesInitFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.Init;
}

public class MoveSelectRunnerInstanceCompleted : StateMigrationSelectRunnerInstanceCompletedBase;
public class MoveSelectRunnerInstanceCancelled : StateMigrationStepResponseBase;
public class MoveSelectRunnerInstanceFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.SelectRunnerInstance;
}
public class MoveGetModuleCompleted : StateMigrationGetModuleCompletedBase;
public class MoveGetModuleCancelled : StateMigrationStepResponseBase;
public class MoveGetModuleFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.GetModule;
}
public class MoveInitCompleted : StateMigrationStepResponseBase;
public class MoveInitCancelled : StateMigrationStepResponseBase;
public class MoveInitFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.Init;
}

public class ImportSelectRunnerInstanceCompleted : StateMigrationSelectRunnerInstanceCompletedBase;
public class ImportSelectRunnerInstanceCancelled : StateMigrationStepResponseBase;
public class ImportSelectRunnerInstanceFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.SelectRunnerInstance;
}
public class ImportGetModuleCompleted : StateMigrationGetModuleCompletedBase;
public class ImportGetModuleCancelled : StateMigrationStepResponseBase;
public class ImportGetModuleFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.GetModule;
}
public class ImportInitCompleted : StateMigrationStepResponseBase;
public class ImportInitCancelled : StateMigrationStepResponseBase;
public class ImportInitFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.Init;
}

public class RemoveSelectRunnerInstanceCompleted : StateMigrationSelectRunnerInstanceCompletedBase;
public class RemoveSelectRunnerInstanceCancelled : StateMigrationStepResponseBase;
public class RemoveSelectRunnerInstanceFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.SelectRunnerInstance;
}
public class RemoveGetModuleCompleted : StateMigrationGetModuleCompletedBase;
public class RemoveGetModuleCancelled : StateMigrationStepResponseBase;
public class RemoveGetModuleFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.GetModule;
}
public class RemoveInitCompleted : StateMigrationStepResponseBase;
public class RemoveInitCancelled : StateMigrationStepResponseBase;
public class RemoveInitFaulted : StateMigrationStepFaultedBase
{
    public override ServerSideStep Step => ServerSideStep.Init;
}
