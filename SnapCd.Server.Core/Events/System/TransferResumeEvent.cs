// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Asks a transfer job that has just left a wait to dispatch the step it was waiting to run. The
/// answer to the wait and the dispatch are two consumes rather than one, so the saga is already in
/// the state that expects the reply before the step is asked for. Publishing both from one chain
/// lets a fast answer arrive while the saga is still in the state it is leaving.
/// </summary>
public class TransferResumeEvent
{
    /// <summary>The job that was waiting.</summary>
    public Guid ModuleJobId { get; set; }

    public Guid OrganizationId { get; set; }
}
