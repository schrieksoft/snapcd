// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Deployment-wide maintenance window flag. A single fixed-key row; enabled means new work is
/// refused and human writes are gated while in-flight jobs drain or park.
/// </summary>
public class MaintenanceMode
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public bool Enabled { get; set; }

    public Guid? EnabledBy { get; set; }

    public DateTime? EnabledAt { get; set; }

    [MaxLength(2000)] public string? Reason { get; set; }

    /// <summary>Null while no window is open.</summary>
    public MaintenancePhase? Phase { get; set; }

    public DateTime? PhaseEnteredAt { get; set; }

    /// <summary>Outcome of the action an acting phase performs, shown as that phase's status.</summary>
    public DateTime? PhaseActionCompletedAt { get; set; }

    [MaxLength(2000)] public string? PhaseActionSummary { get; set; }

    /// <summary>Comma-separated phases the window jumped over, shown as skipped on the timeline.</summary>
    [MaxLength(200)] public string? SkippedPhases { get; set; }
}
