// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ServicePrincipalFederatedCredentials;

/// <summary>
/// DTO for creating a new Service Principal Federated Credential (POST operations).
/// </summary>
public class ServicePrincipalFederatedCredentialCreateDto
{
    /// <summary>ID of the Service Principal this credential authenticates as.</summary>
    public Guid ServicePrincipalId { get; set; }

    /// <summary>Name of the Federated Credential. Must be unique within the Service Principal.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Issuer the presented token must come from, and where its signing keys are published. An absolute https URL, for example a Kubernetes cluster's OIDC issuer.</summary>
    public string Issuer { get; set; } = null!;

    /// <summary>Subject (`sub`) the presented token must carry. For a Kubernetes ServiceAccount this is `system:serviceaccount:{namespace}:{name}`.</summary>
    public string Subject { get; set; } = null!;

    /// <summary>Audience (`aud`) the presented token must carry. Normally `snapcd`.</summary>
    public string Audience { get; set; } = null!;

    /// <summary>Indicates whether the Federated Credential is disabled. A disabled credential is not accepted.</summary>
    public bool IsDisabled { get; set; }
}
