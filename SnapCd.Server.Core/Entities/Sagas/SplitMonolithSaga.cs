// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// State for a SplitMonolith manual job. Derives from JobSagaBase for the plumbing every runner
/// job needs — correlation, concurrency, cancellation, heartbeat, approval, the pinned runner
/// instance — and adds what only this job has.
/// </summary>
public class SplitMonolithSaga : ManualJobSagaBase
{
    /// <summary>Monolith root within the checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? RootDirectory { get; set; }

    /// <summary>Replace a destination whose state does not match. Destructive: state push -force.</summary>
    public bool Overwrite { get; set; }

    /// <summary>
    /// Hash of the refactor map the run proved against. Recorded so a later attempt can tell
    /// whether it is looking at the same split that was approved.
    /// </summary>
    [MaxLength(100)] public string? RefactorMapHash { get; set; }

    /// <summary>Names of the modules the carve produced, as reported by the runner.</summary>
    [MaxLength(4000)] public string? CarvedModuleNames { get; set; }

    /// <summary>Modules the proof covered, and how many planned clean.</summary>
    public int? ProvenModuleCount { get; set; }

}
