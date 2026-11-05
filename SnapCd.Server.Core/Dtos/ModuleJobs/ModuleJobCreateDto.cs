// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos.ModuleJobs;

public class ModuleJobCreateDto
{
    public Guid ModuleId { get; set; }
    public int JobNumber { get; set; }
    public DateTimeOffset TimestampStart { get; set; }
    public DateTimeOffset? TimestampEnd { get; set; }
    public ExecutionStatus Status { get; set; }
    public string JobType { get; set; } = null!;
    public bool? WaitingForApproval { get; set; }
    public bool? IsCurrent { get; set; }
    public string? DefinitiveRevision { get; set; }
    public ActualStateHeadline? ActualStateHeadline { get; set; }
}
