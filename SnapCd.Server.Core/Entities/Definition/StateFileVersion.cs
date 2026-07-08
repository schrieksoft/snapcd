// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

public class StateFileVersion
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StateFileId { get; set; }

    public byte[]? Data { get; set; }

    public Guid CreatedBy { get; set; }
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }
    public DateTime CreatedDateTime { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual StateFile StateFile { get; set; } = null!;
}
