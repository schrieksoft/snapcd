// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Enums;

public enum SystemRole
{
    Administrator
}

/// <summary>
/// Where an open maintenance window has got to. Every window passes through all of them: even
/// with nothing to migrate, the transport is quiesced and has to be brought back.
/// </summary>
public enum MaintenancePhase
{
    /// <summary>Work is finishing: new jobs are held while dispatched tasks run out and the transport empties.</summary>
    Draining,

    /// <summary>Nothing is executing and the transport is idle: the window is ready to be worked in.</summary>
    ReadyForMaintenance,

    /// <summary>Timers are being re-derived from the database.</summary>
    Reconciling,

    /// <summary>Parked jobs are being woken and queued modules re-driven.</summary>
    Resuming
}

public enum QueuedReason
{
    WaitingOnRunningJob,
    WaitingOnDependencies,
    WaitingOnRunnerCheckin,
    Maintenance,
    Paused,

    /// <summary>A client configuring the module (the provider, during an apply) is holding it until it has finished.</summary>
    HeldBySession
}

public enum SecretScope
{
    Stack,
    Namespace,
    Module
}

public enum BusType
{
    AzureServiceBus,
    SqlServer
}

public enum CacheProvider
{
    InMemory,
    Redis
}

public enum AuditPrincipalDiscriminator
{
    User,
    ServicePrincipal,
    System
}

public enum RevisionState
{
    Synced,
    OutOfSync,
    Unknown
}

public enum JobState
{
    Running,
    Idle,
    Unknown
}

/// <summary>
/// Represents a saga state that a job can be in.
/// </summary>
public enum ModuleJobSagaState
{
    Started,

    SelectRunnerInstancePending,
    GetDefinitiveRevisionPending,
    GetModulePending,
    InitPending,
    ValidatePending,
    VariablesPending,
    PlanPending,
    PolicyValidatePending,
    ApplyFromPlanPending,
    OutputPending,


    Completed,
    Faulted,
    Failed,
    Cancelled,

    CancellingImmediateKill,
    CancellingAfterCurrent,
    Declined,
    WaitingForApproval,

    /// <summary>A transfer's own waits, which a reply can arrive during.</summary>
    WaitingForConsent,
    WaitingForFragment,
    WaitingForOutputs
}

public enum ExecutionStatus
{
    Running,
    Completed,
    Cancelled,
    NotApproved,
    Failed,
    Orphaned,
    Unknown,
    PolicyDenied,

    /// <summary>
    /// Some of what the job was asked to do worked and some did not. Unfinished work with a
    /// remedy, rather than a failure: the part that succeeded did succeed.
    /// </summary>
    PartiallyCompleted
}

public enum ActualStateHeadline
{
    Applied,
    Destroyed,

    ApplyFailed,
    ApplyCancelled,
    ApplyTimeout,
    ApplyUnknown,
    ApplyNotApproved,
    ApplyOrphaned,

    DestroyFailed,
    DestroyCancelled,
    DestroyTimeout,
    DestroyUnknown,
    DestroyNotApproved,
    DestroyOrphaned,

    None,

    ApplyPolicyDenied,
    DestroyPolicyDenied
}

/// <summary>
/// Whether a signal could flow along one edge of a wave. Resolves from Potential exactly once,
/// and a consumer probes when none of its edges is Potential any more.
/// </summary>
public enum SignalEdgeStatus
{
    /// <summary>The upstream might signal something this consumer reads. The state at creation.</summary>
    Potential,

    /// <summary>The upstream produced a signal set and this consumer reads something in it.</summary>
    Confirmed,

    /// <summary>
    /// The upstream terminated and there is nothing here for this consumer: it signalled nothing,
    /// or signalled only outputs this consumer does not read. The same answer either way.
    /// </summary>
    NotNeeded,

    /// <summary>
    /// The upstream will never report, because it is paused, refused, or no longer exists. Distinct
    /// from NotNeeded, which is an answer; this is the absence of one, and it releases the consumer
    /// rather than leaving the wave waiting forever.
    /// </summary>
    Unanswerable,

    /// <summary>
    /// This Module was destroyed, so the outputs its consumers read no longer exist. An answer
    /// rather than the absence of one, which is what separates it from Unanswerable.
    /// </summary>
    ModuleDestroyed
}
