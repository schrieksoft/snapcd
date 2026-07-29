// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ServicePrincipals;

/// <summary>
/// DTO for creating a new ServicePrincipal (POST operations).
/// </summary>
public class ServicePrincipalCreateDto
{
    /// <summary>Client Id of the Service Principal. This value must be unique.</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>Client Secret credential. Write-only: supplied on create or update, never returned on reads.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Indicates whether the Service Principal is disabled.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>OAuth scopes granted to tokens issued for this Service Principal. Defaults to `snapcd_scope`.</summary>
    public List<string>? Scopes { get; set; } = ["snapcd_scope"];
}
