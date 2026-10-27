// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Asks a parked apply to look at its contributions again. Published by the job itself on entering
/// the wait, because the answer may already be settled and nothing re-raises it, and by whatever
/// resolves a contribution afterwards.
/// </summary>
public class ProbeSignalsReevaluationRequestedEvent
{
    public Guid ModuleId { get; set; }
    public Guid ModuleJobId { get; set; }
}
