// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

/// <summary>
/// A Module's current and last-completed job state, one row per Module.
///
/// Named for the table rather than ModuleState, which would collide with ModuleStateInfo, the
/// shape the dependency graph reads. Read-only: triggers on ModuleJobs, ModuleSagas and Modules
/// maintain it, along with sp_RecomputeModuleState.
/// </summary>
public class ModuleStateRow
{
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool IsRunning { get; set; }

    /// <summary>The in-flight job, where there is one.</summary>
    public Guid? CurrentModuleJobId { get; set; }

    /// <summary>The most recent job to finish, which is where LatestActualStateHeadline comes from.</summary>
    public Guid? LatestModuleJobId { get; set; }

    public ActualStateHeadline? LatestActualStateHeadline { get; set; }
    public DesiredStateHeadline? DesiredStateHeadline { get; set; }
    public DesiredStateHeadline? QueuedDesiredStateHeadline { get; set; }
}
