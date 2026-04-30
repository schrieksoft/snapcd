using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class QuotaService(IQuotaGatingService quotaGatingService)
{
    /// <summary>
    /// Get the quota limit for a specific organization and quota type.
    /// Returns null if unlimited.
    /// </summary>
    public async Task<int?> GetQuotaAsync(Guid organizationId, string quotaName)
    {
        return await quotaGatingService.GetQuotaAsync(organizationId, quotaName);
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
        var quota = await GetQuotaAsync(organizationId, quotaName);
        if (quota == null)
        {
            return false; // Unlimited
        }
        return currentCount >= quota.Value;
    }
}
