// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Validation;

namespace SnapCd.Server.Core.Startup;

public static class Caching
{
    public static IServiceCollection AddSnapCdCaching(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOptions<CachingSettings>()
            .Bind(configuration.GetSection("Caching"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CachingSettings>, CachingSettingsValidator>();

        var cachingSettings = configuration.GetSection("Caching").Get<CachingSettings>() ?? new CachingSettings();

        switch (cachingSettings.Provider)
        {
            case CacheProvider.Redis:
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
