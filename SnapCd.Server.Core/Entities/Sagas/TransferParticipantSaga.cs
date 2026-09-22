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
/// One participant in a Transfer, alive for as long as the Transfer is. It owns that
/// Module's sequence, its retries and its hold, which is why it outlives any single job: a retry
/// after a failed push is this saga resuming, and the receiver is released at its own verify while
/// the source is still held.
///
/// The TransferSaga tells it what to do next; it reports each result back.
/// </summary>
public class TransferParticipantSaga : ManualJobSagaBase
{
    /// <summary>The Transfer this participant belongs to.</summary>
    public Guid TransferId { get; set; }

    /// <summary>
    /// The instance pinned for this participant is the base's RunnerInstanceName, pinned for the whole
    /// Transfer: its slices exchange files through that instance's checkout, so a second instance
    /// would not have the fragment the first wrote. ModuleId, RunnerId and DeclaredJson likewise
    /// come from the base, naming the runner exactly as any other job does.
    /// </summary>

    /// <summary>Whether this is the source, giving the resources up, or the receiver taking them on.</summary>
    public TransferRole Role { get; set; }

    /// <summary>This participant's root within its own checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? RootDirectory { get; set; }

    /// <summary>The ref this participant was asked to prove, from its consent.</summary>
    [MaxLength(255)] public string? ProveRef { get; set; }

    /// <summary>The commit this participant is proving, resolved from its prove ref by the runner.</summary>
    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    /// <summary>
    /// The fingerprint of the inputs this participant's latest result was produced against. A round whose
    /// key is unchanged reuses the earlier result instead of dispatching.
    /// </summary>
    [MaxLength(64)] public string? InputKey { get; set; }

    /// <summary>The map for the current round, written to the root before each slice.</summary>
    public string? Map { get; set; }

    /// <summary>The fragment this Module was given, if it is the receiver.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }

    /// <summary>The fragment this Module produced, if it is the source.</summary>
    public string? ProducedFragmentState { get; set; }

    public string? ProducedFragmentMeta { get; set; }

    /// <summary>Module names this one needs values from, as the runner read them out of the map.</summary>
    [MaxLength(2000)] public string? NeedsValuesFromJson { get; set; }

    /// <summary>The values the other Module produced that this one consumes, as JSON.</summary>
    public string? OutputsJson { get; set; }

    /// <summary>Stop once the state is pinned, without proving.</summary>
    public bool StopAfterMap { get; set; }

    /// <summary>Zero when it proved clean, 2 when it did not, null when it did not prove.</summary>
    public int? ProveExitCode { get; set; }

    /// <summary>Why it refused, when it did.</summary>
    [MaxLength(2000)] public string? Verdict { get; set; }

    /// <summary>The job the current sequence runs under.</summary>
    public Guid? CurrentJobId { get; set; }

    /// <summary>Which round this participant's latest result belongs to.</summary>
    public int ProveRound { get; set; }
}
