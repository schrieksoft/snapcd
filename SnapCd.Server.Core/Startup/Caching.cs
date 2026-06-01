// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

public static class Caching
{
    public static IServiceCollection AddSnapCdCaching(this IServiceCollection services, ConfigurationManager configuration)
    {
        var cachingSettings = configuration.GetSection("Caching").Get<CachingSettings>() ?? new CachingSettings();
        services.Configure<CachingSettings>(configuration.GetSection("Caching"));

        switch (cachingSettings.Provider)
        {
            case CacheProvider.Redis:
                if (string.IsNullOrEmpty(cachingSettings.ConnectionString))
                    throw new InvalidOperationException("Redis connection string is required when using Redis cache provider");

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cachingSettings.ConnectionString;
                    options.InstanceName = "SnapCd:";
                });
                break;

            case CacheProvider.InMemory:
            default:
                services.AddDistributedMemoryCache();
                break;
        }

        return services;
    }
}
