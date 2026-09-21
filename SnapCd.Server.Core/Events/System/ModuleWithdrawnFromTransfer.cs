// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// A Module's hold must become an ordinary pause: its code and its state disagree until the
/// migration lands, so releasing it outright would let the next ordinary job destroy or duplicate
/// the moved resources.
/// </summary>
public class ModuleWithdrawnFromTransfer
{
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public string Reason { get; set; } = null!;
}
