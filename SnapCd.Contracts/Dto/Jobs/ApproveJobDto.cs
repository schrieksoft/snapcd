// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Jobs;

/// <summary>
/// Body of <c>POST /api/{orgId}/Job/{id}/approve</c>. <see cref="Reason"/> is optional — approve
/// without explanation is allowed (the act of approving signals consent). Distinct from
/// <see cref="DeclineJobDto"/> which requires it.
/// </summary>
public class ApproveJobDto
{
    /// <summary>Optional free-text reason recorded with the approval.</summary>
    public string? Reason { get; set; }
}
