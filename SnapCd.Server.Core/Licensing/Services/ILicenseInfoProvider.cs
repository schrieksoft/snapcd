// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Licensing.Models;

namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// Resolves the <see cref="LicenseInfo"/> for an organization. Implementations are
/// edition-specific: Self-Hosted reads its <c>SelfHostedOrganizationLicense</c> entity and
/// validates the token against the SaaS issuer; SaaS resolves from its own subscription
/// state. Server.Core code consumes this interface rather than <c>LicenseService</c> so it
/// doesn't bind to the Self-Hosted lookup path.
/// </summary>
public interface ILicenseInfoProvider
{
    Task<LicenseInfo> GetLicenseInfoAsync(Guid organizationId);
}
