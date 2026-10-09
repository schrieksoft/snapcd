// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views.Metadata;

/// <summary>
/// What a caller sees of a row it may discover without reading: the identity, and whatever
/// names it. Every metadata view derives from this, so a read that is only permitted to
/// identify a row cannot return anything the full entity carries.
/// </summary>
public abstract class EntityMetadataBase
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
}
