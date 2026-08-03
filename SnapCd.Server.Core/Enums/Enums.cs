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

public enum QueuedReason
{
    WaitingOnRunningJob,
    WaitingOnDependencies,
    WaitingOnRunnerCheckin
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

public enum TaskEndpoint
{
    Started,
    SelectRunnerInstanceCompleted,
    SelectRunnerInstanceCancelled,
    SelectRunnerInstanceFaulted,
    GetDefinitiveRevisionCompleted,
    GetDefinitiveRevisionCancelled,
    GetDefinitiveRevisionFaulted,
    GetModuleCompleted,
    GetModuleCancelled,
    GetModuleFaulted,
    InitCompleted,
    InitCancelled,
    InitFaulted,
    ValidateCompleted,
    ValidateCancelled,
    ValidateFaulted,
    VariablesCompleted,
    VariablesCancelled,
    VariablesFaulted,
    PlanCompleted,
    PlanCancelled,
    PlanFaulted,
    PlanDestroyCompleted,
    PlanDestroyCancelled,
    PlanDestroyFaulted,
    ApplyFromPlanCompleted,
    ApplyFromPlanCancelled,
    ApplyFromPlanFaulted,
    DestroyFromPlanCompleted,
    DestroyFromPlanCancelled,
    DestroyFromPlanFaulted,
    OutputCompleted,
    OutputCancelled,
    OutputFaulted,
    ReportRunningTask,
    CancelKillCompleted,
    CancelGracefulCompleted,
    PolicyValidateCompleted,
    PolicyValidateCancelled,
    PolicyValidateFaulted
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
    ApplyFromPlanPending,
    OutputPending,


    Completed,
    Faulted,
    Failed,
    Cancelled,

    CancellingImmediateKill,
    CancellingImmediateGraceful,
    CancellingAfterCurrent,
    Declined,
    WaitingForApproval
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
    PolicyDenied
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

public enum PreviewFeature
{
    Pulumi
}