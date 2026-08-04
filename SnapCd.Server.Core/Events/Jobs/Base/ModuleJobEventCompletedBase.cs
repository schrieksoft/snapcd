// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Jobs.Base;

public class ModuleJobEventCompletedBase : JobEventBase
{
    public Guid ModuleJobId { get; set; }

    /// <summary>
    /// Why the job ended without applying. Only populated on the cancelled events; lives on the
    /// base because the generic state machine publishes cancelled events through a type parameter.
    /// </summary>
    public CancellationReason? CancellationReason { get; set; }
}