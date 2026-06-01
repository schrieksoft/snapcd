// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Services;

/// <summary>
/// Self-Hosted impl of <see cref="IPremiumEmailPolicy"/>: returns true only when the licence
/// of the single SH organisation includes <see cref="Feature.PremiumEmailProvider"/>.
/// </summary>
public class LicensedPremiumEmailPolicy(LicenseService licenseService) : IPremiumEmailPolicy
{
    public async Task<bool> IsAllowedAsync(CancellationToken ct = default)
    {
        var info = await licenseService.GetLicenseInfoAsync(PreseededSettings.DefaultId);
        return info.Includes(Feature.PremiumEmailProvider);
    }
}
