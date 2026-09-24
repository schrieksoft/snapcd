// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One resource the transfer moves, and where it has got to. Open while it has left one Module and
/// has not been seen in the other.
///
/// Anything may account for it: the transfer's own run, a state move run by hand, or someone saying
/// so. That is why it hangs off the transfer rather than the run that opened it.
/// </summary>
public class TransferObject : AuditBase, IEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid TransferId { get; set; }

    /// <summary>The resource address, as the state names it.</summary>
    [MaxLength(500)] public string Address { get; set; } = null!;

    /// <summary>The Module giving it up, and when its state stopped holding it.</summary>
    public Guid LeftModuleId { get; set; }

    public DateTimeOffset? LeftAt { get; set; }

    /// <summary>The Module taking it on, and when its state was seen holding it.</summary>
    public Guid ArrivedModuleId { get; set; }

    public DateTimeOffset? ArrivedAt { get; set; }

    /// <summary>The job that accounted for it, when a job did rather than a person.</summary>
    public Guid? ResolvedByJobId { get; set; }

    public Guid? ResolvedBy { get; set; }

    public PrincipalDiscriminator? ResolvedByPrincipalDiscriminator { get; set; }

    [MaxLength(500)] public string? ResolvedReason { get; set; }

    [JsonIgnore] public Transfer Transfer { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return TransferId;
    }
}
