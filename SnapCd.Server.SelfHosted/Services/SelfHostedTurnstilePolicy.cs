using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.SelfHosted.Services;

public class SelfHostedTurnstilePolicy : ITurnstilePolicy
{
    public async Task<bool> ShouldEnableTurnstileAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var settings = scope.ServiceProvider.GetRequiredService<IOptions<TurnstileSettings>>().Value;
            if (!settings.IsEnabled) return false;

            var orgIdProvider = scope.ServiceProvider.GetRequiredService<SelfHostedOrganizationIdProvider>();
            var orgId = await orgIdProvider.GetOrganizationIdAsync();
            if (orgId is null) return false;

            var licenseService = scope.ServiceProvider.GetRequiredService<LicenseService>();
            var licenseInfo = await licenseService.GetLicenseInfoAsync(orgId.Value);
            return licenseInfo is { Edition: Edition.EnterpriseEdition, IsValid: true };
        }
        catch
        {
            // DB not available (e.g. design-time EF tooling) — default to Turnstile disabled
            return false;
        }
    }
}
