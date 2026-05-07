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
