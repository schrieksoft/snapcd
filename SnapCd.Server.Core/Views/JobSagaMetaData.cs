// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// Contains essential metadata from a job saga for authorization and validation purposes.
/// </summary>
public class JobSagaMetaData
{
    /// <summary>
    /// Which saga family owns this correlation id. The families have separate state vocabularies, so
    /// the expected state for a step must be looked up in this family's helper and no other.
    /// </summary>
    public JobSagaFamily Family { get; init; } = JobSagaFamily.Deployment;

    public required string CurrentState { get; init; }
    public required Guid RunnerId { get; init; }
    public string? RunnerInstanceName { get; init; }
    public required Guid OrganizationId { get; init; }
    public string? PreviousStateBeforeCancelling { get; init; }
}

/// <summary>
/// The saga families a runner callback can belong to. Apply and Destroy share a state vocabulary
/// (<c>ModuleJobSagaState</c>); SplitMigrate has its own.
/// </summary>
public enum JobSagaFamily
{
    /// <summary>ApplyJobSaga or DestroyJobSaga.</summary>
    Deployment,

    SplitMigrate
}

/// <summary>The outcome of a successful runner authorization: which family owns the job, and its organization.</summary>
public record JobAuthorization(JobSagaFamily Family, Guid OrganizationId);
