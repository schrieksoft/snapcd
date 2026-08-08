// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class MaintenanceModeServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private MaintenanceModeService _service = null!;

    public MaintenanceModeServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddDistributedMemoryCache();
        _provider = services.BuildServiceProvider(true);
        _service = new MaintenanceModeService(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IDistributedCache>(),
            NullLogger<MaintenanceModeService>.Instance);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _service.DisableAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Flag_Defaults_Off_And_Round_Trips()
    {
        Assert.False(await _service.IsActiveAsync());

        var by = Guid.NewGuid();
        await _service.EnableAsync(by, "switch rehearsal");
        Assert.True(await _service.IsActiveAsync());

        var row = await _service.GetAsync();
        Assert.NotNull(row);
        Assert.Equal(by, row!.EnabledBy);
        Assert.Equal("switch rehearsal", row.Reason);
        Assert.NotNull(row.EnabledAt);

        await _service.DisableAsync();
        Assert.False(await _service.IsActiveAsync());
    }

    [Fact]
    public async Task Cache_Is_Written_Through_And_Out_Of_Band_Changes_Self_Heal()
    {
        await _service.DisableAsync();
        Assert.False(await _service.IsActiveAsync());

        // A write bypassing the service (another actor editing the DB) leaves the cache stale,
        // since the cached value has no expiry...
        await using (var db = _fixture.CreateDbContext())
        {
            var row = await db.Set<SnapCd.Server.Core.Entities.Definition.MaintenanceMode>()
                .SingleAsync(m => m.Id == SnapCd.Server.Core.Entities.Definition.MaintenanceMode.SingletonId);
            row.Enabled = true;
            await db.SaveChangesAsync();
        }

        Assert.False(await _service.IsActiveAsync());

        // ...but reading the status repairs it from database truth, so no operator action is needed.
        var status = await _service.GetStatusAsync();
        Assert.True(status.InSync);
        Assert.True(status.Database!.Enabled);
        Assert.True(await _service.IsActiveAsync());

        // A write through the service updates the cache in the same operation.
        await _service.DisableAsync();
        Assert.False(await _service.IsActiveAsync());
        Assert.True((await _service.GetStatusAsync()).InSync);
    }
}
