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
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// The intent: move resources between two Modules, with the counterparty's consent. Which way they
/// move is demonolith's to know, from the transfer map committed alongside the code.
///
/// It outlives any one attempt. A retry is another run under the same intent, and the resources it
/// moves stay accounted for here until the transfer is closed.
/// </summary>
public class Transfer : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>The Module the transfer was started from.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>The Module on the other side of it; its state is written too.</summary>
    public Guid CounterpartyModuleId { get; set; }

    /// <summary>The counterparty's answer, which covers every run under this transfer.</summary>
    public ConsentStatus ConsentStatus { get; set; }

    public Guid? ConsentPrincipalId { get; set; }

    public PrincipalDiscriminator? ConsentPrincipalDiscriminator { get; set; }

    /// <summary>The Agent that consented, when one did rather than a User or plain principal.</summary>
    public Guid? ConsentAgentId { get; set; }

    public DateTimeOffset? ConsentDecidedAt { get; set; }

    [MaxLength(500)] public string? ConsentReason { get; set; }

    /// <summary>
    /// When it was closed. A closed transfer is history: its resources are no longer watched, so
    /// deleting one of them a year later is not mistaken for a move that never landed.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; set; }

    public Guid? ClosedBy { get; set; }

    public PrincipalDiscriminator? ClosedByPrincipalDiscriminator { get; set; }

    [MaxLength(500)] public string? CloseReason { get; set; }

    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public Module CounterpartyModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public List<TransferRun> Runs { get; set; } = null!;
    public List<TransferObject> Objects { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
