// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class QuotaService(IQuotaGatingService quotaGatingService)
{
    /// <summary>
    /// Get how much of a quota'd resource the organization may use.
    /// </summary>
    public async Task<QuotaAllowance> GetAllowanceAsync(Guid organizationId, string quotaName)
    {
        return await quotaGatingService.GetAllowanceAsync(organizationId, quotaName);
    }

    /// <summary>
    /// Get all quota limits for a specific organization.
    /// </summary>
    public async Task<QuotaLimits?> GetQuotaLimitsAsync(Guid organizationId)
    {
        return await quotaGatingService.GetQuotaLimitsAsync(organizationId);
    }

    /// <summary>
    /// Check if an organization has exceeded a specific quota.
    /// </summary>
    public async Task<bool> IsQuotaExceededAsync(Guid organizationId, string quotaName, int currentCount)
    {
        var allowance = await GetAllowanceAsync(organizationId, quotaName);
        return allowance.IsExceededAt(currentCount);
    }
}
