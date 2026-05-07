using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Services;

/// <summary>
/// Self-Hosted impl of <see cref="IPremiumSecretStorePolicy"/>. Returns true when the
/// configured provider is SqlServer (always allowed) or the licence covers PremiumSecretStore.
/// Decision cached for 60s in IMemoryCache.
/// </summary>
public class LicensedPremiumSecretStorePolicy(
    LicenseService licenseService,
    IOptions<SecretStoreSettings> secretStoreSettings,
    IMemoryCache cache) : IPremiumSecretStorePolicy
{
    private const string CacheKey = "premium-secret-store-allowed";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    public async Task<bool> IsAllowedAsync(CancellationToken ct = default)
    {
        if (secretStoreSettings.Value.Provider == SecretStoreProvider.SqlServer) return true;

        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            var info = await licenseService.GetLicenseInfoAsync(PreseededSettings.DefaultId);
            return info.Includes(Feature.PremiumSecretStore);
        });
    }
}
