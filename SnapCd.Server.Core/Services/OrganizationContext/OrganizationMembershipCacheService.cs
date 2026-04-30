using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.OrganizationContext;

public class OrganizationMembershipCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public OrganizationMembershipCacheService(
        IDistributedCache cache,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _cache = cache;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid organizationId)
    {
        var cacheKey = GetCacheKey(userId, organizationId);

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return cached == "1";

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var isMember = await dbContext.OrganizationUsers
            .AnyAsync(ou => ou.UserId == userId
                            && ou.OrganizationId == organizationId
                            && !ou.IsDeactivated
                            && ou.InvitationCompleted);

        await _cache.SetStringAsync(cacheKey, isMember ? "1" : "0", CacheOptions);

        return isMember;
    }

    public async Task InvalidateAsync(Guid userId, Guid organizationId)
    {
        var cacheKey = GetCacheKey(userId, organizationId);
        await _cache.RemoveAsync(cacheKey);
    }

    private static string GetCacheKey(Guid userId, Guid organizationId)
        => $"org-membership:{userId}:{organizationId}";
}
