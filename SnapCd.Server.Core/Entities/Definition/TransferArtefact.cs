// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// What the source produces at its map step and the receiver needs before its own. demonolith runs
/// one root at a time, so the two halves never see each other's working directory and the files
/// have to travel.
///
/// Encrypted with the service that encrypts state files, because a fragment is raw state and
/// carries whatever state carries. Deleted when the transfer closes; not a durable record.
/// </summary>
public class TransferArtefact : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>The transfer whose two halves these belong to.</summary>
    public Guid TransferId { get; set; }

    /// <summary>
    /// The moving resources' state, cut from the source's pulled state by `state mv`, with the
    /// meta that pins it to the state it came from. Encrypted; never logged, never sent to a page.
    /// </summary>
    public string? SourceFragmentCiphertext { get; set; }

    /// <summary>The fragment's meta, which the receiver checks against the map before applying.</summary>
    public string? SourceFragmentMetaCiphertext { get; set; }

    /// <summary>The output values the counterpart's plan reads, as demonolith writes them.</summary>
    public string? ReceiverOutputsCiphertext { get; set; }

    [JsonIgnore] public Transfer Transfer { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId() => TransferId;
}
