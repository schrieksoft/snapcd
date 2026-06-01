// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One row per secret processed by a Secret Migrator run. Grouped by <see cref="RunId"/>.
/// </summary>
public class SecretMigrationAudit : AuditBase
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public DateTime RunStartedUtc { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid ExecutedByUserId { get; set; }

    [Required, MaxLength(64)] public required string Direction { get; set; }
    [Required, MaxLength(512)] public required string Name { get; set; }
    [Required, MaxLength(32)] public required string Action { get; set; }
    [Required, MaxLength(16)] public required string Kind { get; set; }

    [MaxLength(2048)] public string? ErrorMessage { get; set; }
}
