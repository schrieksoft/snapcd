// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Host.Licensing.Services;
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
