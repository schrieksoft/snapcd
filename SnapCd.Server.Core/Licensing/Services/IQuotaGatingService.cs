using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Licensing.Services;

public interface IQuotaGatingService
{
    Task<int?> GetQuotaAsync(Guid organizationId, string quotaName);
    Task<QuotaLimits?> GetQuotaLimitsAsync(Guid organizationId);
}
