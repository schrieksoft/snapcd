// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One attempt at a transfer: a job on each Module it covers, run at once. Many per transfer, since
/// finishing a Module whose move failed is another attempt at the same intent.
/// </summary>
public class TransferRun : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid TransferId { get; set; }

    /// <summary>Whether this attempt covers both Modules or only the one it was started from.</summary>
    public TransferScope Scope { get; set; }

    /// <summary>The ref the starting Module runs against.</summary>
    [MaxLength(255)] public string? Ref { get; set; }

    /// <summary>The ref the counterparty runs against.</summary>
    [MaxLength(255)] public string? CounterpartyRef { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    [JsonIgnore] public Transfer Transfer { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return TransferId;
    }
}
