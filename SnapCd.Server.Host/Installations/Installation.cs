// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Host.Installations;

/// <summary>
/// Deployment-wide identity and what the licensing service last told this installation. A single
/// fixed-key row inserted by the migration; the seed follows the database, so replicas share it and a restored backup keeps it.
/// </summary>
public class Installation
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public Guid SeedGuid { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    [MaxLength(64)] public string? LatestKnownVersion { get; set; }
    public DateTime? LatestKnownVersionCheckedAtUtc { get; set; }
    public DateTime? LastTelemetryReportAtUtc { get; set; }
    /// <summary>Serialised What's New entries as last received, newest first.</summary>
    public string? WhatsNewJson { get; set; }
}
