using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Licensing.Services;

public class QuotaGatingService(LicenseService licenseService) : IQuotaGatingService
{
    public async Task<int?> GetQuotaAsync(Guid organizationId, string quotaName)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);

        if (licenseInfo is { Edition: Edition.EnterpriseEdition, IsValid: true })
        {
            // EE: unlimited everything except modules
            if (quotaName == nameof(QuotaLimits.ModuleQuota))
                return licenseInfo.MaxModules;

            return null;
        }

        // CE: hardcoded limits
        return quotaName switch
        {
            nameof(QuotaLimits.StackQuota) => 2,
            nameof(QuotaLimits.RunnerQuota) => 2,
            nameof(QuotaLimits.ModuleQuota) => 20,
            _ => null
        };
    }

    public async Task<QuotaLimits?> GetQuotaLimitsAsync(Guid organizationId)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);

        if (licenseInfo is { Edition: Edition.EnterpriseEdition, IsValid: true })
        {
            // EE: only module quota from license
            return new QuotaLimits
            {
                ModuleQuota = licenseInfo.MaxModules
            };
        }

        // CE: hardcoded limits
        return new QuotaLimits
        {
            StackQuota = 2,
            RunnerQuota = 2,
            ModuleQuota = 20
        };
    }
}
