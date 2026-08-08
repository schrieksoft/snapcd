// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class TransportProbeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    public TransportProbeTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create the real MassTransit SQL-transport schema in the fixture database, so the
        // probe's raw SQL is validated against whatever the installed MassTransit version ships.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<SqlTransportOptions>().Configure(o => o.ConnectionString = _fixture.ConnectionString);
        services.AddSqlServerMigrationHostedService(create: true, delete: false);
        _provider = services.BuildServiceProvider(true);
        foreach (var hostedService in _provider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Sql_Transport_Probe_Matches_The_Shipped_Schema()
    {
        var settings = new ServiceBusSettings
        {
            BusType = BusType.SqlServer,
            TransportOptions = new TransportOptionsSettings
            {
                SqlServer = new SqlTransportOptions { ConnectionString = _fixture.ConnectionString }
            }
        };
        var service = new TransportProbeService(
            Options.Create(settings),
            new ConfigurationBuilder().Build());

        var depths = await service.GetDepthsAsync();

        Assert.Null(depths.ProbeError);
        Assert.Equal("SqlServer", depths.Provider);
        Assert.True(depths.IsTransportQuiet);
    }

    [Fact]
    public void Diagnostic_Queues_Are_Excluded_From_The_Gate()
    {
        var depths = new TransportDepths("SqlServer",
        [
            new QueueDepth("plan-queue", 0, 5, 2, 1),
            new QueueDepth("plan-queue_error", 7, 0, 0, 0),
            new QueueDepth("saga-queue_skipped", 3, 0, 0, 0)
        ], null);
        Assert.True(depths.IsTransportQuiet);

        var busy = depths with { Queues = [new QueueDepth("plan-queue", 1, 0, 0, 0)] };
        Assert.False(busy.IsTransportQuiet);
    }
}
