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
/// A move of resources from one Module into one other: the map that describes it, and both sides'
/// gates. A transfer has exactly two sides, so they are columns rather than rows - moving into two
/// Modules is two Transfers, which is also all demonolith's map allows.
///
/// The Transfer outlives any one job, because consent, locks and merge declarations are gathered
/// before a job runs and survive between the prove and the migrate.
/// </summary>
public class Transfer : AuditBase, IEntity
{
    /// <summary>The transfer's id.</summary>
    public Guid Id { get; set; }

    /// <summary>The organization both Modules belong to.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The Module the resources move out of; its owners initiate and close the Transfer.</summary>
    public Guid SourceModuleId { get; set; }

    /// <summary>The Module the resources move into; its owners consent before anything runs.</summary>
    public Guid ReceiverModuleId { get; set; }

    /// <summary>The transfer map, as demonolith wrote it.</summary>
    public string MapJson { get; set; } = null!;

    /// <summary>The map's content hash: the transfer's identity, and what consent is given against.</summary>
    [MaxLength(64)] public string MapHash { get; set; } = null!;

    /// <summary>Where the transfer stands, derived from the gates below and the jobs run under it.</summary>
    public TransferStatus Status { get; set; }

    /// <summary>When the transfer closed, however it ended.</summary>
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Why it closed.</summary>
    [MaxLength(500)] public string? CloseReason { get; set; }

    // The source's gates. It consents by initiating, so it has no consent columns.

    /// <summary>The ref the source wants proved; changeable until it locks.</summary>
    [MaxLength(255)] public string? SourceProveRef { get; set; }

    /// <summary>When the Transfer took its hold on the source; null once released.</summary>
    public DateTimeOffset? SourceLockedAt { get; set; }

    /// <summary>Who locked the source.</summary>
    public Guid? SourceLockedBy { get; set; }

    /// <summary>Whether that was a User or a ServicePrincipal.</summary>
    public PrincipalDiscriminator? SourceLockedByPrincipalDiscriminator { get; set; }

    /// <summary>When the source's proved code was declared merged.</summary>
    public DateTimeOffset? SourceMergedDeclaredAt { get; set; }

    /// <summary>Who declared the source merged.</summary>
    public Guid? SourceMergedDeclaredBy { get; set; }

    /// <summary>Whether that was a User or a ServicePrincipal.</summary>
    public PrincipalDiscriminator? SourceMergedDeclaredByPrincipalDiscriminator { get; set; }

    /// <summary>The commit the source's proved code landed as.</summary>
    [MaxLength(100)] public string? SourceMergedCommit { get; set; }

    /// <summary>When the source's hold was lifted, by landing or by withdrawal.</summary>
    public DateTimeOffset? SourceReleasedAt { get; set; }

    // The receiver's gates, which start with consent.

    /// <summary>The ref the receiver wants proved; changeable until it locks.</summary>
    [MaxLength(255)] public string? ReceiverProveRef { get; set; }

    /// <summary>Whether the receiver has authorised this transfer, against the current map hash.</summary>
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

    /// <summary>When the Transfer took its hold on the receiver; null once released.</summary>
    public DateTimeOffset? ReceiverLockedAt { get; set; }

    /// <summary>Who locked the receiver.</summary>
    public Guid? ReceiverLockedBy { get; set; }

    /// <summary>Whether that was a User or a ServicePrincipal.</summary>
    public PrincipalDiscriminator? ReceiverLockedByPrincipalDiscriminator { get; set; }

    /// <summary>When the receiver's proved code was declared merged.</summary>
    public DateTimeOffset? ReceiverMergedDeclaredAt { get; set; }

    /// <summary>Who declared the receiver merged.</summary>
    public Guid? ReceiverMergedDeclaredBy { get; set; }

    /// <summary>Whether that was a User or a ServicePrincipal.</summary>
    public PrincipalDiscriminator? ReceiverMergedDeclaredByPrincipalDiscriminator { get; set; }

    /// <summary>The commit the receiver's proved code landed as.</summary>
    [MaxLength(100)] public string? ReceiverMergedCommit { get; set; }

    /// <summary>When the receiver's hold was lifted, by landing or by withdrawal.</summary>
    public DateTimeOffset? ReceiverReleasedAt { get; set; }

    [JsonIgnore] public Module SourceModule { get; set; } = null!;
    [JsonIgnore] public Module ReceiverModule { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return SourceModuleId;
    }
}
