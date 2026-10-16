// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Something in a wave changed: a contribution resolved, or a participant recorded its signals. A
/// wave moves while someone is looking at it, and a view that only updates on reload makes a
/// working wave look stuck.
/// </summary>
public class ProbeWaveUpdatedEvent
{
    public Guid ProbeWaveId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The Module whose participation changed, where one did.</summary>
    public Guid? ModuleId { get; set; }
}
