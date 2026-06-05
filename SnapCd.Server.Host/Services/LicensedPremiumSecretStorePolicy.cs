// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Host.Licensing.Services;
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
