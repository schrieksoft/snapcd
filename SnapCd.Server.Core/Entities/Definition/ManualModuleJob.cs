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

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// An operator-initiated job that runs against a paused Module. Kept separate from ModuleJob
/// because IsCurrent there drives dependency resolution and the gatekeeper's dequeue check, and
/// manual work is neither a deployment nor a reason for dependents to wait.
/// </summary>
public class ManualModuleJob : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    public int JobNumber { get; set; }

    public DateTimeOffset TimestampStart { get; set; }
    public DateTimeOffset? TimestampEnd { get; set; }

    public ExecutionStatus Status { get; set; }

    [MaxLength(100)] public string JobType { get; set; } = null!;

    public bool? WaitingForApproval { get; set; }

    public ServerSideStep? FailedOnServerSideStep { get; set; }

    [MaxLength(255)] public string? ServerSideErrorHeader { get; set; }

    [MaxLength(16000)] public string? ServerSideError { get; set; }

    public string? Logs { get; set; }

    public List<ManualModuleJobApproval> ManualModuleJobApprovals { get; set; } = null!;

    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
