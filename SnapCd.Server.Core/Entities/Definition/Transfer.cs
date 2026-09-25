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
/// Two Modules agreeing to move resources between their states. It coordinates nothing: each side
/// runs its own job against its own ref. What it holds is the agreement - who asked, who answered,
/// and which refs they answered for.
///
/// A Module is in at most one open transfer, in either role. That is enforced by TransferLocks,
/// which a trigger maintains from this table.
/// </summary>
public class Transfer : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>The Module the transfer was started from.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>The Module asked to agree to it.</summary>
    public Guid CounterpartyModuleId { get; set; }

    public ConsentStatus ConsentStatus { get; set; }

    public DateTimeOffset? ConsentDecidedAt { get; set; }

    public Guid? ConsentPrincipalId { get; set; }

    public PrincipalDiscriminator? ConsentPrincipalDiscriminator { get; set; }

    /// <summary>The ref each side runs against, named as that side committed to it.</summary>
    [MaxLength(255)] public string? ModuleRef { get; set; }

    [MaxLength(255)] public string? CounterpartyRef { get; set; }

    /// <summary>
    /// Set once both sides' jobs have ended, or when consent is refused. Closing releases both
    /// Modules, so a further attempt is a new transfer rather than a re-run of this one.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; set; }

    public Guid? ClosedBy { get; set; }

    [MaxLength(500)] public string? CloseReason { get; set; }

    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public Module CounterpartyModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId() => ModuleId;
}
