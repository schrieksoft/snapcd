// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;
using MassTransit;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// One per Transfer, alive from the moment it is opened until it lands or is abandoned. It runs
/// prove rounds as either branch moves, then the migration once both sides have locked and merged,
/// so the handover from proving to migrating is a transition rather than a handshake between two
/// jobs.
///
/// It does not run any step itself: each participant has its own saga that owns its sequence and
/// its retries. This one decides what happens next - which side proves first, who holds the
/// fragment, when the approval gate opens, and which side pushes.
/// </summary>
public class TransferSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    [MaxLength(255)] public string CurrentState { get; set; } = null!;

    public byte[] RowVersion { get; set; } = null!;

    public Guid OrganizationId { get; set; }


    /// <summary>The Transfer this coordinates; the saga is correlated on its id.</summary>
    public Guid TransferId { get; set; }

    public Guid SourceModuleId { get; set; }

    public Guid ReceiverModuleId { get; set; }

    /// <summary>
    /// The map hash the current round is working against. A map edit changes it, which is what
    /// makes every proof from the previous round stale.
    /// </summary>
    [MaxLength(64)] public string? MapHash { get; set; }

    /// <summary>
    /// The prove round this is, counted from one. Steps cite it so a later round's verdict is not
    /// confused with an earlier one's.
    /// </summary>
    public int ProveRound { get; set; }

    /// <summary>
    /// The manual job the current round or migration runs under, so its logs and steps have a job
    /// to hang off. Null between rounds, when the Transfer is merely open.
    /// </summary>
    public Guid? CurrentJobId { get; set; }

    /// <summary>
    /// Why the run stopped, when it stalled. A stall keeps the holds: it is waiting for a person,
    /// not failing.
    /// </summary>
    [MaxLength(2000)] public string? StallReason { get; set; }
}
