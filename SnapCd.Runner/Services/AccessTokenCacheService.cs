using Microsoft.Extensions.Caching.Memory;
using SnapCd.Runner.Constants;

namespace SnapCd.Runner.Services;

public class AccessTokenCacheService
{
    private readonly IMemoryCache _cache;

    public AccessTokenCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string? Get()
    {
        if (_cache.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out string? accessToken)) return accessToken;

        return null;
    }
}