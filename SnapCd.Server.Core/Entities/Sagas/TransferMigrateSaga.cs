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
/// One Module's state move within a transfer run, an ordinary manual job on that Module's runner.
/// The Modules in a run move in parallel; what actually moved is recorded in the transfer's ledger.
/// </summary>
public class TransferMigrateSaga : ManualJobSagaBase
{
    /// <summary>
    /// The Module on the other side of the move. Held only so a plan that reads a value the other
    /// side produces knows whose outputs to wait for; nothing coordinates the two.
    /// </summary>
    public Guid CounterpartyModuleId { get; set; }

    /// <summary>The transfer this job is one side of, when it was started under one.</summary>
    public Guid? TransferId { get; set; }

    /// <summary>This Module's root within its own checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? RootDirectory { get; set; }

    /// <summary>The ref this Module runs against: the source's from the initiator, the receiver's from its consent.</summary>
    [MaxLength(255)] public string? ProveRef { get; set; }

    /// <summary>The commit that ref resolved to, recorded against the work.</summary>
    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    /// <summary>
    /// Output names this Module's plan consumes from the counterparty, as JSON. Set at map time and
    /// cleared once they are all available.
    /// </summary>
    [MaxLength(2000)] public string? NeedsOutputsJson { get; set; }

    /// <summary>Whether the counterparty's outputs are all available, re-read on every check.</summary>
    public bool HasOutputs { get; set; }

    /// <summary>Zero when the plan came out clean, 2 when it did not.</summary>
    public int? ProveExitCode { get; set; }

    /// <summary>Why demonolith refused, when it did.</summary>
    [MaxLength(2000)] public string? Verdict { get; set; }
}
