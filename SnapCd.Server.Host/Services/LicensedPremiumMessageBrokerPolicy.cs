using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Services;

/// <summary>
/// Self-Hosted impl of <see cref="IPremiumMessageBrokerPolicy"/>. Returns true when the
/// configured bus is SqlServer (always allowed) or the licence covers PremiumMessageBroker.
/// Decision cached for 60s in IMemoryCache to keep the per-job-creation hot path cheap.
/// </summary>
public class LicensedPremiumMessageBrokerPolicy(
    LicenseService licenseService,
    IOptions<ServiceBusSettings> serviceBusSettings,
    IMemoryCache cache) : IPremiumMessageBrokerPolicy
{
    private const string CacheKey = "premium-bus-allowed";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    public async Task<bool> IsAllowedAsync(CancellationToken ct = default)
    {
        if (serviceBusSettings.Value.BusType == BusType.SqlServer) return true;

        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            var info = await licenseService.GetLicenseInfoAsync(PreseededSettings.DefaultId);
            return info.Includes(Feature.PremiumMessageBroker);
        });
    }
}
