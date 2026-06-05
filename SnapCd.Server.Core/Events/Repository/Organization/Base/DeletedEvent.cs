// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Events.Repository.Organization.Base;

/// <summary>
/// Delete Event Transfer Object that contains a DTO with audit metadata for delete operations.
/// Includes organization context for multi-tenant scenarios.
/// </summary>
public class DeletedEvent<TDto>
{
    /// <summary>
    /// The DTO payload containing the business data
    /// </summary>
    public TDto Data { get; set; } = default!;

    /// <summary>
    /// Organization ID for multi-tenant context
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// ID of the principal that created this entity
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Type of principal that created this entity (User or ServicePrincipal)
    /// </summary>
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// AgentId of the Agent that created this entity (acting via the underlying ServicePrincipal),
    /// or null if the creator was not an Agent.
    /// </summary>
    public Guid? CreatedByAgentId { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was created
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// ID of the principal that last modified this entity
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// Type of principal that last modified this entity (User or ServicePrincipal)
    /// </summary>
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// AgentId of the Agent that last modified this entity (acting via the underlying ServicePrincipal),
    /// or null if the modifier was not an Agent.
    /// </summary>
    public Guid? ModifiedByAgentId { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified
    /// </summary>
    public DateTime ModifiedDateTime { get; set; }
}
