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
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// A move of resources from one Module into one other, and the receiver's consent to it. What
/// moves is decided in the code by the transfer map, which is committed alongside it and read by
/// demonolith from each Module's own checkout - the server never carries it.
///
/// It records no state of its own. Where the transfer stands is read from its jobs - one per
/// Module - so nothing here can fall out of step with what actually ran.
/// </summary>
public class Transfer : AuditBase, IEntity
{
    /// <summary>The transfer's id.</summary>
    public Guid Id { get; set; }

    /// <summary>The organization both Modules belong to.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The Module the resources move out of; its owners start the transfer.</summary>
    public Guid SourceModuleId { get; set; }

    /// <summary>The Module the resources move into; its owners consent before anything runs.</summary>
    public Guid ReceiverModuleId { get; set; }

    /// <summary>The ref the source runs against.</summary>
    [MaxLength(255)] public string? SourceProveRef { get; set; }

    /// <summary>The ref the receiver runs against, from its consent.</summary>
    [MaxLength(255)] public string? ReceiverProveRef { get; set; }

    /// <summary>
    /// Which Modules this transfer moves. A side that failed is finished by a transfer covering
    /// that side alone.
    /// </summary>
    public TransferScope Scope { get; set; }

    /// <summary>The receiving Module's answer: receiving state into it is its owners' decision.</summary>
    public ConsentStatus ReceiverConsentStatus { get; set; }

    /// <summary>Who answered for the receiver.</summary>
    public Guid? ReceiverConsentPrincipalId { get; set; }

    /// <summary>Whether that was a User or a ServicePrincipal, recorded as approvals record it.</summary>
    public PrincipalDiscriminator? ReceiverConsentPrincipalDiscriminator { get; set; }

    /// <summary>
    /// AgentId of the Agent that consented (acting via its underlying ServicePrincipal), or
    /// <c>null</c> when a User or a non-agent ServicePrincipal did.
    /// </summary>
    public Guid? ReceiverConsentAgentId { get; set; }

    /// <summary>When the receiver answered.</summary>
    public DateTimeOffset? ReceiverConsentDecidedAt { get; set; }

    /// <summary>What the receiver said alongside its decision.</summary>
    [MaxLength(500)] public string? ReceiverConsentReason { get; set; }

    [JsonIgnore] public Module SourceModule { get; set; } = null!;
    [JsonIgnore] public Module ReceiverModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return SourceModuleId;
    }
}
