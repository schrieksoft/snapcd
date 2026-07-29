// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Integrations;

/// <summary>
/// Read projection of an <c>Integration</c> row — mirrors the entity's identity/routing fields. The
/// connection (credentials + config) lives encrypted in the secret backend and is never on this DTO; the
/// redacted connection is exposed separately on <see cref="IntegrationDetailDto"/>.
/// </summary>
public class IntegrationReadDto : IDto
{
    /// <summary>Unique ID of the integration.</summary>
    public Guid Id { get; set; }
    /// <summary>ID of the Organization the Integration belongs to.</summary>
    public Guid OrganizationId { get; set; }
    /// <summary>Name of the integration.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Integration type (e.g. Slack).</summary>
    public IntegrationType IntegrationType { get; set; }
    /// <summary>Whether the integration is enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>Whether the integration is supplied org-wide.</summary>
    public bool IsSuppliedToAllModules { get; set; }
}

/// <summary>
/// Single-integration read that adds the connection as a freeform JSON string, with secret fields masked.
/// Secrets are never returned in clear.
/// </summary>
public class IntegrationDetailDto : IntegrationReadDto
{
    /// <summary>The connection as a freeform JSON string, with secret fields masked. Type-specific shape;
    /// the type's codec interprets it. Never contains a clear secret.</summary>
    public string? Connection { get; set; }
}
