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
/// One Module's state move. A transfer is two of these, one per Module, each an ordinary manual job
/// on its own Module's runner. Nothing coordinates them: the source's job waits for the receiver's
/// to have completed, and demonolith refuses to strip the source without its run receipt.
/// </summary>
public class TransferMigrateSaga : ManualJobSagaBase
{
    /// <summary>The Transfer this job belongs to; both jobs point at the same one.</summary>
    public Guid TransferId { get; set; }

    /// <summary>Whether this Module gives the resources up or takes them on.</summary>
    public TransferRole Role { get; set; }

    /// <summary>This Module's root within its own checkout, passed as --root-dir.</summary>
    [MaxLength(1000)] public string? RootDirectory { get; set; }

    /// <summary>The ref this Module runs against: the source's from the initiator, the receiver's from its consent.</summary>
    [MaxLength(255)] public string? ProveRef { get; set; }

    /// <summary>The commit that ref resolved to, recorded against the work.</summary>
    [MaxLength(255)] public string? DefinitiveRevision { get; set; }

    /// <summary>The fragment this Module was given, when it is the receiver.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }

    /// <summary>The fragment this Module produced, when it is the source.</summary>
    public string? ProducedFragmentState { get; set; }

    public string? ProducedFragmentMeta { get; set; }

    /// <summary>The values the other Module produced that this one consumes, as JSON.</summary>
    public string? OutputsJson { get; set; }

    /// <summary>Zero when the plan came out clean, 2 when it did not.</summary>
    public int? ProveExitCode { get; set; }

    /// <summary>Why demonolith refused, when it did.</summary>
    [MaxLength(2000)] public string? Verdict { get; set; }
}
