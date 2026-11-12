// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.UI.Dashboard.Components.Approvals;

/// <summary>
/// Where a job stands on its approval. The decision and the threshold come from the job row, so a
/// finished job reports what applied to it when it was decided.
/// </summary>
/// <param name="Decision">Null where the job never waited for an approval.</param>
/// <param name="Threshold">The count the job was held to, or null where none was recorded.</param>
/// <param name="IsWaiting">Whether a decision is being waited on now.</param>
public record ApprovalState(ApprovalDecision? Decision, int? Threshold, bool IsWaiting);

/// <summary>
/// Everything an approvals view needs that differs between the job families: which table the
/// decisions live in, which job they hang off, and where the threshold comes from. The view
/// itself is identical for both, so it takes one of these rather than knowing either family.
/// </summary>
public interface IApprovalSource
{
    Task<IReadOnlyList<IJobApproval>> ListApprovals(Guid jobId, Guid moduleId, Guid organizationId);

    /// <summary>
    /// The job's approval outcome, the threshold it was held to, and whether it is waiting now, in
    /// one read. The live threshold is the fallback for a job with none recorded.
    /// </summary>
    Task<ApprovalState> GetApprovalState(Guid jobId, Guid moduleId, Guid organizationId);

    Task Approve(Guid jobId, Guid moduleId, Guid organizationId);

    Task Decline(Guid jobId, Guid moduleId, Guid organizationId);

    /// <summary>Whether the current principal may approve or decline this job.</summary>
    Task<bool> CanDecide(Guid jobId, Guid moduleId, Guid organizationId);

    /// <summary>Withdrawing a decision is offered only where the family supports it.</summary>
    bool CanRemoveApproval { get; }

    /// <summary>What the panel is deciding on, for the dialog wording: "Plan", "Split", and so on.</summary>
    string DecisionNoun { get; }

    Task RemoveApproval(Guid approvalId, Guid organizationId);
}
