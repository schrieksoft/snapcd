// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ServicePrincipals;

/// <summary>
/// DTO for updating an existing ServicePrincipal (PUT operations).
/// </summary>
public class ServicePrincipalUpdateDto : ServicePrincipalCreateDto, IUpdateDto
{
    /// <summary>Unique ID of the Service Principal.</summary>
    public Guid Id { get; set; }

    /// <summary>When true, clears the stored Client Secret, leaving the Service Principal able to authenticate only through a Federated Credential. An empty `client_secret` on its own leaves the stored secret in place, so removal has to be asked for.</summary>
    public bool RemoveClientSecret { get; set; }
}
