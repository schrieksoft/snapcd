// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Licensing.Services;

public interface IApprovalPolicy
{
    /// <summary>
    /// Returns true when the organization's tier includes the ApprovalWorkflows feature
    /// (i.e. approvals can be enforced before apply/destroy). When false, the org cannot
    /// use approval thresholds — jobs auto-approve when no threshold is configured, or
    /// fail with NotApproved when one is.
    /// </summary>
    Task<bool> SupportsApprovalWorkflowsAsync(Guid organizationId);
}
