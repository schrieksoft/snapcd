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
    CancelGracefulCompleted
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
    Unknown
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

    None
}

public enum PreviewFeature
{
    Pulumi
}