// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Entities.Definition;

public class SelfHostedOrganizationLicense : AuditBase
{
    public Guid OrganizationId { get; set; }
    public virtual Organization? Organization { get; set; }

    [MaxLength(4096)] public string? LicenseToken { get; set; }

    [MaxLength(255)] public string? SelfHostedLicenseKey { get; set; }

    public Guid? SelfHostedSubscriptionId { get; set; }

    // Cached RSA public key used to validate LicenseToken. Fetched from snapcd.io periodically.
    [MaxLength(4096)] public string? PublicKeyPem { get; set; }

    public DateTime? PublicKeyFetchedAtUtc { get; set; }
}
