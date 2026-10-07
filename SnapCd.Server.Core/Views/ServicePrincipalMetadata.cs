// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>Id and name of a Service Principal, for callers who may pick one without reading it.</summary>
public class ServicePrincipalMetadata
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Without the organization prefix the client id is stored with.</summary>
    public string ClientId { get; set; } = null!;

    public string? DisplayName { get; set; }
}
