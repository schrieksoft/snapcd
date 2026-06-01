// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
