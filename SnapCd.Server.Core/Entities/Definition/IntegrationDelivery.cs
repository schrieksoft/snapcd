// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Definition;

public enum IntegrationDeliveryStatus
{
    Pending,
    Sent,
    Failed
}

/// <summary>
/// Audit record of one attempt to deliver a trigger occurrence to an integration. Also the idempotency
/// ledger (unique <c>DedupeKey + IntegrationEventId</c>) and the Slack thread-root store (<c>MessageId</c>
/// per mission). Plain entity (no AuditBase) — written by the background dispatcher, not a user request.
/// </summary>
public class IntegrationDelivery
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid IntegrationEventId { get; set; }
    public IntegrationTrigger Trigger { get; set; }

    public Guid? ModuleJobId { get; set; }
    public Guid? ModuleJobMissionId { get; set; }

    /// <summary>Identifies the trigger occurrence; unique together with <see cref="IntegrationEventId"/> so a
    /// redelivered bus message doesn't double-send.</summary>
    [MaxLength(300)] public string DedupeKey { get; set; } = null!;

    public IntegrationDeliveryStatus Status { get; set; }

    /// <summary>Sink message id (e.g. the Slack message <c>ts</c>) — the thread root for a mission's milestones.</summary>
    [MaxLength(200)] public string? MessageId { get; set; }

    [MaxLength(2000)] public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }
}
