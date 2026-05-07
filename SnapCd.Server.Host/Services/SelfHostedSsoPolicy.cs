using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Host.Services;

public class SelfHostedSsoPolicy : ISsoPolicy
{
    public async Task<bool> ShouldEnableSsoAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var orgIdProvider = scope.ServiceProvider.GetRequiredService<SelfHostedOrganizationIdProvider>();
            var orgId = await orgIdProvider.GetOrganizationIdAsync();
            if (orgId is null) return false;

            var licenseService = scope.ServiceProvider.GetRequiredService<LicenseService>();
            var licenseInfo = await licenseService.GetLicenseInfoAsync(orgId.Value);
            return licenseInfo.Includes(Feature.Sso);
        }
        catch
        {
            // DB not available (e.g. design-time EF tooling) — default to SSO disabled
            return false;
        }
    }
}
