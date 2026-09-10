// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Host.Installations;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class InstallationServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    public InstallationServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SelfHostedSnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddSingleton<IDbContextFactory<SnapCdDbContext>>(sp => new SelfHostedFactory(sp.GetRequiredService<IDbContextFactory<SelfHostedSnapCdDbContext>>()));
        _provider = services.BuildServiceProvider(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    private InstallationService Service() =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>());

    [Fact]
    public async Task Seed_ExistsAfterMigration_AndIsStable()
    {
        var first = await Service().GetAsync();
        var second = await Service().GetAsync();

        Assert.NotEqual(Guid.Empty, first.SeedGuid);
        Assert.Equal(first.SeedGuid, second.SeedGuid);
        Assert.Equal(Installation.SingletonId, first.Id);

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(1, await db.Set<Installation>().CountAsync());
    }

    [Fact]
    public async Task Update_PersistsWhatTheLicensingServiceSaid()
    {
        var service = Service();
        var seedBefore = (await service.GetAsync()).SeedGuid;

        await service.UpdateAsync(i =>
        {
            i.LatestKnownVersion = "9.9.9";
            i.LatestKnownVersionCheckedAtUtc = DateTime.UtcNow;
            i.WhatsNewJson = "[]";
        });

        var after = await service.GetAsync();
        Assert.Equal("9.9.9", after.LatestKnownVersion);
        Assert.Equal("[]", after.WhatsNewJson);
        Assert.Equal(seedBefore, after.SeedGuid);
    }

    private sealed class SelfHostedFactory(IDbContextFactory<SelfHostedSnapCdDbContext> inner) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => inner.CreateDbContext();
    }
}
