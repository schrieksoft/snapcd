// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class AuditBase
{
    // Audit fields
    public Guid CreatedBy { get; set; }

    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }

    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }

    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }

    public DateTime ModifiedDateTime { get; set; }
}