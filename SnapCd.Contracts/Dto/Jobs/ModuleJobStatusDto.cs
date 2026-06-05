// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Jobs;

/// <summary>
/// Status metadata for a ModuleJob: JobType (Apply/Destroy), WaitingForApproval, ActualStateHeadline,
/// server-side error fields, and output deltas (OutputsCreate/Modify/Destroy/Recreate/Unchanged lists).
/// Does NOT contain the resource-action plan body or the apply output — those are in module_job_logs
/// filtered by TaskName.
/// </summary>
public class ModuleJobStatusDto
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public string JobType { get; init; } = string.Empty;
    public bool? WaitingForApproval { get; init; }
    public string? ActualStateHeadline { get; init; }
    public string? ServerSideErrorHeader { get; init; }
    public string? ServerSideError { get; init; }
    public string? OutputsUnchangedList { get; init; }
    public string? OutputsCreateList { get; init; }
    public string? OutputsModifyList { get; init; }
    public string? OutputsDestroyList { get; init; }
    public string? OutputsRecreateList { get; init; }
}
