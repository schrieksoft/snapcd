// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleJobApprovals;

public class ModuleJobApprovalCreateDto
{
    public Guid ModuleJobId { get; set; }
    public Guid PrincipalId { get; set; }
    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }
    public DateTime DecisionDateTime { get; set; }
    public bool Declined { get; set; }
}
