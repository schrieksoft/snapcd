// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.CallerContext;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;
using CallerCtx = SnapCd.Server.Core.Services.CallerContext.CallerContext;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class CallerContextAndGateTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private MaintenanceModeService _maintenance = null!;
    private Microsoft.Extensions.DependencyInjection.ServiceProvider _provider = null!;

    public CallerContextAndGateTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions
            .AddDbContextFactory<SnapCdDbContext>(services, o => o.UseSqlServer(_fixture.ConnectionString));
        Microsoft.Extensions.DependencyInjection.MemoryCacheServiceCollectionExtensions.AddDistributedMemoryCache(services);
        _provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services, true);
        _maintenance = new MaintenanceModeService(
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(_provider),
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IDistributedCache>(_provider),
            NullLogger<MaintenanceModeService>.Instance);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        MaintenanceGate.Reset();
        await _maintenance.DisableAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public void Scopes_Nest_And_Restore()
    {
        Assert.False(CallerCtx.IsExempt);
        using (CallerCtx.Begin(CallerKind.Runner))
        {
            Assert.Equal(CallerKind.Runner, CallerCtx.Kind);
            using (CallerCtx.Begin(CallerKind.System))
            {
                Assert.Equal(CallerKind.System, CallerCtx.Kind);
            }

            Assert.Equal(CallerKind.Runner, CallerCtx.Kind);
        }

        Assert.False(CallerCtx.IsExempt);
    }

    [Fact]
    public async Task Scope_Flows_Across_Awaits_And_Isolates_Parallel_Flows()
    {
        async Task<CallerKind?> Flow(CallerKind kind)
        {
            using var _ = CallerCtx.Begin(kind);
            await Task.Delay(50);
            return CallerCtx.Kind;
        }

        var runner = Flow(CallerKind.Runner);
        var agent = Flow(CallerKind.Agent);
        Assert.Equal(CallerKind.Runner, await runner);
        Assert.Equal(CallerKind.Agent, await agent);
        Assert.False(CallerCtx.IsExempt);
    }

    [Fact]
    public async Task Gate_Blocks_Human_Writes_And_Passes_Scoped_Ones()
    {
        var module = _fixture.Modules["0000"];

        Guid jobId;
        await using (var db = _fixture.CreateDbContext())
        {
            jobId = Guid.NewGuid();
            db.ModuleJobs.Add(new ModuleJob
            {
                Id = jobId,
                OrganizationId = module.OrganizationId,
                ModuleId = module.Id,
                TimestampStart = DateTimeOffset.UtcNow,
                Status = ExecutionStatus.Running,
                JobType = "Apply",
                IsCurrent = null
            });
            await db.SaveChangesAsync();
        }

        MaintenanceGate.Initialize(_maintenance);
        try
        {
            await _maintenance.EnableAsync(Guid.NewGuid(), "gate test");

            await using var db = _fixture.CreateDbContext();
            var repo = new ModuleJobRepository(
                db,
                new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [module.OrganizationId]),
                Mock.Of<IPublishEndpoint>(),
                Options.Create(new ModuleJobRepositorySettings()));

            var entity = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
            entity.PlanTotalChangedCount = 7;

            // Human (no scope): refused.
            await Assert.ThrowsAsync<MaintenanceModeException>(() => repo.ExecuteUpdate(entity));

            // Runner scope: passes.
            using (CallerCtx.Begin(CallerKind.Runner))
            {
                await repo.ExecuteUpdate(entity);
            }

            // Window closed: human write passes again.
            await _maintenance.DisableAsync();
            await repo.ExecuteUpdate(entity);
        }
        finally
        {
            MaintenanceGate.Reset();
            await _maintenance.DisableAsync();
        }
    }
}

public class CallerContextConsumeFilterTests
{
    private static CallerKind? _observedKind;

    public class Probe
    {
        public Guid Id { get; set; }
    }

    public class ProbeConsumer : IConsumer<Probe>
    {
        public Task Consume(ConsumeContext<Probe> context)
        {
            _observedKind = CallerCtx.Kind;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Every_Consumer_Runs_In_A_System_Scope()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        MassTransit.DependencyInjectionTestingExtensions.AddMassTransitTestHarness((Microsoft.Extensions.DependencyInjection.IServiceCollection)services, x =>
        {
            x.AddConsumer<ProbeConsumer>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.UseConsumeFilter(typeof(CallerContextConsumeFilter<>), ctx);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        await using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services, true);
        var harness = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MassTransit.Testing.ITestHarness>(provider);
        await harness.Start();

        _observedKind = null;
        await harness.Bus.Publish(new Probe { Id = Guid.NewGuid() });
        Assert.True(await harness.Consumed.Any<Probe>());
        Assert.Equal(CallerKind.System, _observedKind);
        Assert.False(CallerCtx.IsExempt);
    }
}
