// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// One participant's side of a Transfer, alive for as long as the Transfer is. It owns that
/// Module's sequence, its retries and its hold, which is why it outlives any single job: a retry
/// after a failed push is this saga resuming, and the receiver is released at its own verify while
/// the source is still held.
///
/// The TransferSaga tells it what to do next; it reports each result back.
/// </summary>
public class TransferParticipantSaga : ManualJobSagaBase
{
    /// <summary>The Transfer this side belongs to.</summary>
    public Guid TransferId { get; set; }

    /// <summary>
    /// The instance pinned for this side is the base's RunnerInstanceName, pinned for the whole
    /// Transfer: its slices exchange files through that instance's checkout, so a second instance
    /// would not have the fragment the first wrote. ModuleId, RunnerId and DeclaredJson likewise
    /// come from the base, naming the runner exactly as any other job does.
    /// </summary>

    /// <summary>Whether this is the side giving the resources up or taking them on.</summary>
    public TransferRole Role { get; set; }

    /// <summary>This side's root within its own checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? RootDirectory { get; set; }

    /// <summary>The commit this side is proving, resolved from its prove ref by the runner.</summary>
    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    /// <summary>
    /// The fingerprint of the inputs this side's latest result was produced against. A round whose
    /// key is unchanged reuses the earlier result instead of dispatching.
    /// </summary>
    [MaxLength(64)] public string? InputKey { get; set; }

    /// <summary>The job the current sequence runs under.</summary>
    public Guid? CurrentJobId { get; set; }

    /// <summary>Which round this side's latest result belongs to.</summary>
    public int ProveRound { get; set; }
}
