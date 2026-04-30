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
