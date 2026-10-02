// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.UI.Dashboard.Components.Approvals;

/// <summary>
/// Everything an approvals view needs that differs between the job families: which table the
/// decisions live in, which job they hang off, and where the threshold comes from. The view
/// itself is identical for both, so it takes one of these rather than knowing either family.
/// </summary>
public interface IApprovalSource
{
    Task<IReadOnlyList<IJobApproval>> ListApprovals(Guid jobId, Guid moduleId, Guid organizationId);

    Task<bool> IsWaitingForApproval(Guid jobId, Guid moduleId, Guid organizationId);

    /// <summary>How many approvals the job needs, or null when the family has no threshold.</summary>
    Task<int?> ResolveThreshold(Guid jobId, Guid moduleId, Guid organizationId);

    Task Approve(Guid jobId, Guid moduleId, Guid organizationId);

    Task Decline(Guid jobId, Guid moduleId, Guid organizationId);

    /// <summary>Withdrawing a decision is offered only where the family supports it.</summary>
    bool CanRemoveApproval { get; }

    /// <summary>What the panel is deciding on, for the dialog wording: "Plan", "Split", and so on.</summary>
    string DecisionNoun { get; }

    Task RemoveApproval(Guid approvalId, Guid organizationId);
}
