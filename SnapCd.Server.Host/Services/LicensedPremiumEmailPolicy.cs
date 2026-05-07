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
