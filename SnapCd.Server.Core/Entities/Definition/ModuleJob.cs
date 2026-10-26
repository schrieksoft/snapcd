// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleJob : AuditBase, IEntity, IModuleChild, IJobWithLogs
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    public int JobNumber { get; set; }

    public DateTimeOffset TimestampStart { get; set; }
    public DateTimeOffset? TimestampEnd { get; set; }

    public ExecutionStatus Status { get; set; }

    [MaxLength(100)] public string JobType { get; set; } = null!;


    public ServerSideStep? FailedOnServerSideStep { get; set; }

    [MaxLength(255)] public string? ServerSideErrorHeader { get; set; }

    [MaxLength(16000)] public string? ServerSideError { get; set; }

    public string? Logs { get; set; }

    public bool? WaitingForApproval { get; set; }
    /// <summary>
    /// The runner this job was dispatched to. Recorded on the job rather than only on the saga,
    /// which is deleted when the job finishes, and carried without a foreign key so it survives
    /// the runner being removed.
    /// </summary>
    public Guid? SelectedRunnerId { get; set; }

    public bool? IsCurrent { get; set; }

    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    [MaxLength(100)] public string? DefinitiveClosureHash { get; set; }

    /// <summary>
    /// What this job ran against, for judging whether a prediction made from it still holds. The
    /// declaration covers everything resolved for the Module; the two input hashes cover the values
    /// that were resolved, and are written separately because parameters and environment variables
    /// resolve through their own resolver at their own step.
    /// </summary>
    [MaxLength(64)] public string? DeclarationHash { get; set; }

    [MaxLength(64)] public string? ParamsHash { get; set; }

    [MaxLength(64)] public string? EnvVarsHash { get; set; }

    /// <summary>
    /// Why this job's plan started no propagation, where it should have. The apply is unaffected,
    /// so the job is not failed; what is lost is the answer an approver would have had, and the
    /// record is what lets them ask for it again.
    /// </summary>
    [MaxLength(1000)] public string? PropagationError { get; set; }

    public ActualStateHeadline? ActualStateHeadline { get; set; }

    public PolicyOutcome? PolicyOutcome { get; set; }

    public int? PlanTotalChangedCount { get; set; }
    public int? PlanCreateCount { get; set; }
    public int? PlanModifyCount { get; set; }
    public int? PlanDestroyCount { get; set; }
    public int? PlanRecreateCount { get; set; }

    [MaxLength(4000)] public string? OutputsUnchangedList { get; set; }
    [MaxLength(4000)] public string? OutputsCreateList { get; set; }
    [MaxLength(4000)] public string? OutputsModifyList { get; set; }
    [MaxLength(4000)] public string? OutputsDestroyList { get; set; }
    [MaxLength(4000)] public string? OutputsRecreateList { get; set; }

    /// <summary>
    /// What this plan said its outputs would become, in the format a downstream root consumes.
    /// Kept because a propagation started from it can be asked for again after the plan message
    /// is gone. Absent for an engine whose plan carries no encoded outputs.
    /// </summary>
    [MaxLength(16000)] public string? OutputSignalsJson { get; set; }

    public List<ModuleJobApproval> ModuleJobApprovals { get; set; } = null!;
    public List<ModuleJobMission> ModuleJobMissions { get; set; } = null!;

    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}

public enum ServerSideStep
{
    Start,
    SelectRunnerInstance,
    GetDefinitiveRevision,
    GetModule,
    Init,
    Validate,
    Variables,
    Plan,
    ApplyProbe,
    DestroyProbe,
    ApplyFromPlan,
    DestroyFromPlan,
    Output,
    Approval,
    PolicyValidate,
    Move,
    MoveDryRun,
    Import,
    ImportPreCheck,
    Remove,
    RemoveDryRun,
    LookupAddresses,
    SplitPlan,
    SplitPlanEmptyVerify,
    SplitRefactorValidate,
    SplitRefactorDiff,
    SplitMigrateMap,
    SplitMigrateProve,
    SplitMigrateRun,
    SplitMigrateVerify,
    AnalyseTransferRefactorMap,
    TransferMigrateMap,
    TransferMigrateProve,
    TransferMigrateRun,
    TransferMigrateVerify
}