using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Host.Database;

public static class SelfHostedDbContextConfiguration
{
    public static IServiceCollection AddSelfHostedDbContextConfiguration(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<SelfHostedSnapCdDbContext>(options =>
        {
            options.UseSqlServer(connectionString,
                m => { m.MigrationsHistoryTable("__EFMigrationsHistory"); });
            options.UseOpenIddict();
            options.UseOpenIddict<ServicePrincipal, Authorization, Scope, Token, Guid>();
        });

        // IDbContextFactory is singleton by convention (AddDbContextFactory registers it as such).
        // Must stay singleton so singleton consumers (e.g. OrganizationMembershipCacheService) can use it.
        services.AddSingleton<IDbContextFactory<SnapCdDbContext>>(sp =>
            new BaseDbContextFactoryAdapter(sp.GetRequiredService<IDbContextFactory<SelfHostedSnapCdDbContext>>()));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<SelfHostedSnapCdDbContext>());

        return services;
    }

    private sealed class BaseDbContextFactoryAdapter(IDbContextFactory<SelfHostedSnapCdDbContext> inner) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => inner.CreateDbContext();
        public async Task<SnapCdDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            await inner.CreateDbContextAsync(ct);
    }
}
