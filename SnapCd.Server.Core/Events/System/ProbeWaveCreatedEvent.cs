// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// A propagation episode has begun. Raised once per wave, where <see cref="ProbeWaveUpdatedEvent"/>
/// is raised repeatedly as one progresses, so a reader counting waves wants this one.
/// </summary>
public class ProbeWaveCreatedEvent
{
    public Guid ProbeWaveId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The Module whose outputs changed, which is what a wave is counted against.</summary>
    public Guid OriginModuleId { get; set; }
}
