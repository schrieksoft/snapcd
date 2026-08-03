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

public class ModuleJob : AuditBase, IEntity, IModuleChild
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

    public bool? IsCurrent { get; set; }

    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    [MaxLength(100)] public string? DefinitiveClosureHash { get; set; }

    public ActualStateHeadline? ActualStateHeadline { get; set; }

    public PolicyOutcome? PolicyOutcome { get; set; }

    [MaxLength(4000)] public string? OutputsUnchangedList { get; set; }
    [MaxLength(4000)] public string? OutputsCreateList { get; set; }
    [MaxLength(4000)] public string? OutputsModifyList { get; set; }
    [MaxLength(4000)] public string? OutputsDestroyList { get; set; }
    [MaxLength(4000)] public string? OutputsRecreateList { get; set; }

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
    ApplyFromPlan,
    DestroyFromPlan,
    Output,
    Approval
}