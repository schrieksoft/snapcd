// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Asks the Module's saga to take a Transfer's hold: from then on triggers park rather than
/// dispatch. Published when a participant locks, before any job exists to wait on it.
/// </summary>
public class ModuleHoldRequested
{
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
}

/// <summary>
/// Asks the Module's saga to release a Transfer's hold and re-drive parked work. Ignored unless
/// the id matches the hold on record.
/// </summary>
public class ModuleReleaseRequested
{
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
}
