// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Jobs;

/// <summary>
/// Status metadata for a ModuleJob: JobType (Apply/Destroy), DefinitiveRevision (the resolved commit
/// the job ran against), WaitingForApproval, ActualStateHeadline, server-side error fields, and output
/// deltas (OutputsCreate/Modify/Destroy/Recreate/Unchanged lists). Does NOT contain the resource-action
/// plan body or the apply output — those are in module_job_logs filtered by TaskName.
/// </summary>
public class ModuleJobStatusDto
{
    /// <summary>Unique ID of the Job.</summary>
    public Guid Id { get; init; }
    /// <summary>ID of the Module the Job ran against.</summary>
    public Guid ModuleId { get; init; }
    /// <summary>Type of the Job: Apply or Destroy.</summary>
    public string JobType { get; init; } = string.Empty;
    /// <summary>The resolved commit the Job ran against.</summary>
    public string? DefinitiveRevision { get; init; }
    /// <summary>True while the Job is waiting for an approval decision.</summary>
    public bool? WaitingForApproval { get; init; }
    /// <summary>Headline of the Module's actual state after the Job (e.g. Applied, Destroyed, Failed).</summary>
    public string? ActualStateHeadline { get; init; }
    /// <summary>Short server-side error header, when the Job failed outside runner execution.</summary>
    public string? ServerSideErrorHeader { get; init; }
    /// <summary>Full server-side error detail, when present.</summary>
    public string? ServerSideError { get; init; }
    /// <summary>Names of outputs left unchanged by the Job.</summary>
    public string? OutputsUnchangedList { get; init; }
    /// <summary>Names of outputs created by the Job.</summary>
    public string? OutputsCreateList { get; init; }
    /// <summary>Names of outputs modified by the Job.</summary>
    public string? OutputsModifyList { get; init; }
    /// <summary>Names of outputs destroyed by the Job.</summary>
    public string? OutputsDestroyList { get; init; }
    /// <summary>Names of outputs recreated by the Job.</summary>
    public string? OutputsRecreateList { get; init; }
}
