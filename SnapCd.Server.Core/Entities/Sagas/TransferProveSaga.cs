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
/// State for a TransferProve manual job: the two-Module counterpart of SplitProve. Read-only and
/// repeatable - it takes no hold, writes no state, and can be re-run as either branch moves.
///
/// Unlike a split, every step names which participant it is for, so the saga pins a runner for each
/// side: the source's is the one JobSagaBase carries, the receiver's is its own pair below.
/// </summary>
public class TransferProveSaga : ManualJobSagaBase
{
    /// <summary>The Transfer this job runs under; its rows carry the refs and the consent.</summary>
    public Guid TransferId { get; set; }

    /// <summary>The Module the resources move out of.</summary>
    public Guid SourceModuleId { get; set; }

    /// <summary>The Module they move into.</summary>
    public Guid ReceiverModuleId { get; set; }

    /// <summary>
    /// The runner pinned for the receiver. The source's is the RunnerId and RunnerInstanceName the
    /// base carries, since the job is owned by the source Module.
    ///
    /// Each side is pinned for the whole job, because its slices exchange files through that
    /// instance's checkout: a second instance would not have the fragment the first wrote.
    /// </summary>
    public Guid ReceiverRunnerId { get; set; }

    /// <summary>The receiver's pinned instance.</summary>
    [MaxLength(255)] public string? ReceiverRunnerInstanceName { get; set; }

    /// <summary>Root directory within the source's checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? SourceRootDirectory { get; set; }

    /// <summary>Root directory within the receiver's checkout.</summary>
    [MaxLength(1000)] public string? ReceiverRootDirectory { get; set; }

    /// <summary>
    /// The map hash the run proved against, recorded so a verdict can be tied to the map that
    /// produced it rather than to whatever the Transfer carries now.
    /// </summary>
    [MaxLength(64)] public string? MapHash { get; set; }
}
