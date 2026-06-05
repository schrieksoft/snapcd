// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Jobs;

/// <summary>
/// Body of <c>POST /api/{orgId}/Job/{id}/decline</c>. <see cref="Reason"/> is required — declining
/// without explanation is bad practice; the API enforces what good practice already expects.
/// </summary>
public class DeclineJobDto
{
    public string Reason { get; set; } = null!;
}
