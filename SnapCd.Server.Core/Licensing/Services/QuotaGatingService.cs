// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Licensing.Services;

public class QuotaGatingService(LicenseService licenseService) : IQuotaGatingService
{
    public async Task<int?> GetQuotaAsync(Guid organizationId, string quotaName)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);

        // Modules are the only resource gated by license tier today;
        // every other quota is unlimited regardless of tier.
        if (quotaName == nameof(QuotaLimits.ModuleQuota))
        {
            return licenseInfo.MaxModules;
        }

        return null;
    }

    public async Task<QuotaLimits?> GetQuotaLimitsAsync(Guid organizationId)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);

        return new QuotaLimits
        {
            ModuleQuota = licenseInfo.MaxModules
        };
    }
}
