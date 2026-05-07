using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Services;

public class SelfHostedOrganizationCountValidator : IOrganizationCountValidator
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly object _lock = new();
    private DateTime _lastCheck = DateTime.MinValue;
    private bool _isOverLimit;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public SelfHostedOrganizationCountValidator(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsOverLimitAsync()
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastCheck < CacheDuration)
                return _isOverLimit;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var orgCount = await dbContext.Organizations.CountAsync(
            o => o.DeletedDateTime == null && o.Id != PreseededSettings.DefaultId);

        lock (_lock)
        {
            _isOverLimit = orgCount > 1;
            _lastCheck = DateTime.UtcNow;
        }

        return _isOverLimit;
    }
}
