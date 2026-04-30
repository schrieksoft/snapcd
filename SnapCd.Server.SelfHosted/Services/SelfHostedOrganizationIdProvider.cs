using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.SelfHosted.Services;

/// <summary>
/// Caches the id of the (single) self-hosted organization in process memory.
/// On a self-hosted install the org id is effectively constant once seeded, so
/// policies that need it (SSO, Turnstile, etc.) should not DB-read it on every request.
/// </summary>
public class SelfHostedOrganizationIdProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Guid? _cachedOrgId;
    private DateTime _cachedAtUtc;

    public SelfHostedOrganizationIdProvider(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid?> GetOrganizationIdAsync()
    {
        // Fast path: cached, non-null, not expired.
        if (_cachedOrgId is { } cached && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
            return cached;

        await _gate.WaitAsync();
        try
        {
            if (_cachedOrgId is { } afterWait && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
                return afterWait;

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var orgId = await dbContext.Organizations
                .Where(o => o.DeletedDateTime == null)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync();

            // Only cache non-null results. If the org doesn't exist yet (e.g. pre-seed),
            // we re-query on subsequent calls until it's there — then cache for 24h.
            if (orgId is not null)
            {
                _cachedOrgId = orgId;
                _cachedAtUtc = DateTime.UtcNow;
            }

            return orgId;
        }
        catch
        {
            // DB unavailable (design-time tooling, transient outage). Don't poison the cache.
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }
}
