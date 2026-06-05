// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

/// <summary>
/// One Service Principal to materialise via the data seeder on Server startup. Used both by
/// the production-time preseed (e.g. the default Runner SP) and the debug-time seeder (extra
/// SPs for developer workstations).
/// </summary>
public class ServicePrincipalToSeed
{
    /// <summary>Optional fixed ID. When null, a fresh GUID is generated.</summary>
    public Guid? Id { get; set; }

    /// <summary>OAuth2 client identifier the SP authenticates with.</summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// OAuth2 client secret. Sensitive — source via the External Settings provider in production.
    /// Null is permitted only for SPs that authenticate by other means (e.g. introspection-based).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// OpenIddict client type — typically <c>confidential</c> for server-to-server SPs.
    /// </summary>
    public required string ClientType { get; set; }

    /// <summary>OpenIddict consent type (e.g. <c>implicit</c>, <c>explicit</c>, <c>external</c>).</summary>
    public required string? ConsentType { get; set; }

    /// <summary>Human-readable display name shown in the Dashboard's SP list.</summary>
    public required string? DisplayName { get; set; }

    /// <summary>OAuth2 login redirect URI registered against the SP.</summary>
    public required string? LoginRedirectUri { get; set; }

    /// <summary>OAuth2 post-logout redirect URI registered against the SP.</summary>
    public required string? LogoutRedirectUri { get; set; }

    /// <summary>
    /// When true (default), the SP is created as a regular Service Principal. When false, the
    /// row is treated as a non-SP OAuth client (rare; intended for system applications).
    /// </summary>
    public bool IsServicePrincipal { get; set; } = true;

    /// <summary>OAuth2 scopes the SP is permitted to request.</summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Organization the SP belongs to. Empty (the default) maps to the system / null organization
    /// — for SPs that aren't tied to a single tenant.
    /// </summary>
    public Guid OrganizationId { get; set; } = Guid.Empty; // Default to NULL organization for system apps
}