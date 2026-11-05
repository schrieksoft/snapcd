// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model for ModuleJob used for component state persistence.
/// Contains only the properties needed for UI rendering without navigation properties.
/// </summary>
public class ModuleJobView
{
    public required Guid Id { get; init; }
    public required int JobNumber { get; init; }
    public required DateTimeOffset TimestampStart { get; init; }
    public DateTimeOffset? TimestampEnd { get; set; }
    public required ExecutionStatus Status { get; set; }
    public required string JobType { get; init; }
    public ServerSideStep? FailedOnServerSideStep { get; set; }
    public string? ServerSideErrorHeader { get; set; }
    public string? ServerSideError { get; set; }
    public bool? WaitingForApproval { get; set; }
}