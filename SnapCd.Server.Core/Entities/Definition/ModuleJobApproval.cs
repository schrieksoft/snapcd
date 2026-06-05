// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleJobApproval : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ModuleJobId { get; set; }

    public Guid PrincipalId { get; set; }

    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }

    /// <summary>
    /// AgentId of the Agent that decided this approval (acting via its underlying ServicePrincipal),
    /// or <c>null</c> if the decision was made by a User or a non-agent ServicePrincipal.
    /// <see cref="PrincipalId"/> is still the underlying SP (load-bearing for the approval-creation
    /// guard); a non-null AgentId is the sole signal that an Agent made the decision.
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>Human-supplied (or agent-supplied) rationale for the approve / decline decision.
    /// Required on decline (enforced by the controller), optional on approve.</summary>
    [MaxLength(2000)] public string? Reason { get; set; }

    public DateTime DecisionDateTime { get; set; }

    public bool Declined { get; set; }

    [JsonIgnore] public ModuleJob ModuleJob { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleJobId;
    }
}