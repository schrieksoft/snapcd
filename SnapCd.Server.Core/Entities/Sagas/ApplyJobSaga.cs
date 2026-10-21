// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// Only an apply probes, so the wave a probe is answering lives here rather than on the shared
/// base: the other families would carry a column they never write.
/// </summary>
public class ApplyJobSaga : JobSagaBase
{
    /// <summary>
    /// The wave the probe now in flight is answering. A job probes once per wave it owes, so the
    /// attempt rows alone cannot say which of them the next reply belongs to.
    /// </summary>
    public Guid? ProbingForWaveId { get; set; }
}