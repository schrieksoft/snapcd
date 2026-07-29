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
    /// <summary>ID of the Job the approval decision applies to.</summary>
    public Guid ModuleJobId { get; set; }
    /// <summary>ID of the User or Service Principal that made the decision.</summary>
    public Guid PrincipalId { get; set; }
    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }
    /// <summary>When the decision was recorded (UTC).</summary>
    public DateTime DecisionDateTime { get; set; }
    /// <summary>True if the decision was a decline, false if an approval.</summary>
    public bool Declined { get; set; }
}
