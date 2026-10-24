// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class RunnerConnectionLivenessTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly Guid _serverId = Guid.NewGuid();
    private readonly OpenConnections<RunnerHub> _open = new();
    private readonly List<Guid> _seeded = [];
    private ServiceProvider _provider = null!;
    private Runner _runner = null!;

    public RunnerConnectionLivenessTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _runner = _fixture.Runners["0"];
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddMassTransitTestHarness();
        _provider = services.BuildServiceProvider(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
            await db.RunnerConnections.Where(c => _seeded.Contains(c.Id)).ExecuteDeleteAsync();
        await _provider.DisposeAsync();
    }

    private RunnerConnectionRepositoryFactory Repositories() =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IBus>(),
            Options.Create(new RunnerConnectionRepositorySettings()));

    private RunnerSelectionService Selection() =>
        new(Repositories(),
            _fixture.CreateDbContext(),
            Options.Create(new ServerSettings { InstanceId = _serverId }),
            _open,
            NullLogger<RunnerSelectionService>.Instance);

    private async Task<RunnerConnection> SeedConnection(string? instance = null)
    {
        var connection = new RunnerConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = _runner.OrganizationId,
            RunnerId = _runner.Id,
            InstanceName = instance ?? $"liveness-{Guid.NewGuid():N}",
            SignalRConnectionId = Guid.NewGuid().ToString("N"),
            ServerInstanceId = _serverId
        };
        _seeded.Add(connection.Id);

        await using var db = _fixture.CreateDbContext();
        db.RunnerConnections.Add(connection);
        await db.SaveChangesAsync();
        return connection;
    }

    private async Task<bool> Exists(Guid connectionRecordId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.RunnerConnections.AnyAsync(c => c.Id == connectionRecordId);
    }

    [Fact]
    public async Task A_runner_whose_connection_is_open_is_selected_without_a_round_trip()
    {
        var connection = await SeedConnection();
        _open.Add(connection.SignalRConnectionId);

        var selected = await Selection().SelectSpecificRunnerAsync(_runner.OrganizationId, _runner.Id, connection.InstanceName);

        Assert.Equal(connection.Id, selected?.Id);
        Assert.True(await Exists(connection.Id));
    }

    [Fact]
    public async Task A_row_whose_connection_is_closed_is_removed_and_not_selected()
    {
        var connection = await SeedConnection();

        var selected = await Selection().SelectSpecificRunnerAsync(_runner.OrganizationId, _runner.Id, connection.InstanceName);

        Assert.Null(selected);
        Assert.False(await Exists(connection.Id));
    }

    [Fact]
    public async Task Removing_a_checked_row_leaves_the_row_a_reconnect_wrote_in_its_place()
    {
        var stale = await SeedConnection();
        await using (var db = _fixture.CreateDbContext())
            await db.RunnerConnections.Where(c => c.Id == stale.Id).ExecuteDeleteAsync();
        var fresh = await SeedConnection(stale.InstanceName);

        using var repository = Repositories().Create();
        await repository.DeleteConnectionRecord(stale.Id);

        Assert.True(await Exists(fresh.Id));
    }

    [Fact]
    public async Task A_call_on_an_unregistered_connection_closes_it_and_is_retryable()
    {
        var authorization = new RunnerJobAuthorizationService(
            Repositories(),
            null!,
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            NullLogger<RunnerJobAuthorizationService>.Instance);
        var context = new CallerContext(Guid.NewGuid().ToString("N"), _runner.OrganizationId);

        var ex = await Assert.ThrowsAsync<HubException>(() => authorization.ValidateIsForCurrentConnection(context, Guid.NewGuid()));

        Assert.True(context.Aborted);
        Assert.True(HubErrorCodes.IsRetryable(ex.Message));
    }

    private sealed class CallerContext(string connectionId, Guid organizationId) : HubCallerContext
    {
        public bool Aborted { get; private set; }

        public override string ConnectionId { get; } = connectionId;

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User { get; } = new(new ClaimsIdentity(
            [new Claim(ClaimTypeConstants.OrganizationClaimType, organizationId.ToString())], "Bearer"));

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = BuildFeatures(organizationId);

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort() => Aborted = true;

        private static IFeatureCollection BuildFeatures(Guid organizationId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?organization_id={organizationId}");
            var features = new FeatureCollection();
            features.Set<IHttpContextFeature>(new HttpContextFeature { HttpContext = httpContext });
            return features;
        }

        private sealed class HttpContextFeature : IHttpContextFeature
        {
            public HttpContext? HttpContext { get; set; }
        }
    }
}
