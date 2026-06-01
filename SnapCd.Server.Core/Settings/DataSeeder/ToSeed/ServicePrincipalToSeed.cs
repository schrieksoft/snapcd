// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

public class ServicePrincipalToSeed
{
    public Guid? Id { get; set; }
    public required string ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public required string ClientType { get; set; }
    public required string? ConsentType { get; set; }
    public required string? DisplayName { get; set; }
    public required string? LoginRedirectUri { get; set; }
    public required string? LogoutRedirectUri { get; set; }
    public bool IsServicePrincipal { get; set; } = true;
    public List<string> Scopes { get; set; } = new();
    public Guid OrganizationId { get; set; } = Guid.Empty; // Default to NULL organization for system apps
}