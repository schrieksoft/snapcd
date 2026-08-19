// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Entities.Sagas.Base;

/// <summary>
/// Shared state for operator-initiated jobs. Adds to JobSagaBase the one thing every manual job
/// needs and no deployment job has: a reason a job stopped without breaking.
/// </summary>
public class ManualJobSagaBase : JobSagaBase
{
    /// <summary>
    /// Why the job stopped short of its goal, when nothing failed. A tool that ran correctly and
    /// answered "no", or a precondition that was not met. Set instead of an error so the job ends
    /// with a stated reason rather than as breakage.
    /// </summary>
    [MaxLength(2000)] public string? NegativeVerdict { get; set; }
}
