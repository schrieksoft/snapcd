// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

using ApplyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.ApplyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.PlanRequested,
    SnapCd.Server.Core.Events.Steps.PlanCompleted,
    SnapCd.Server.Core.Events.Steps.PlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCancelled>;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class StuckJobDetectionTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public StuckJobDetectionTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddSingleton<SnapCd.Server.Core.Services.MaintenanceMode.IMaintenanceModeService, SnapCd.Server.Core.Services.MaintenanceMode.MaintenanceModeService>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<ApplyMachine, ApplyJobSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    private StuckJobDetectionService CreateService()
        => new(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            Options.Create(new StuckJobDetectionSettings()));

    private async Task<Guid> SeedSaga(string state, DateTime? waitingSince)
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            IsCurrent = null
        });
        var declared = new ResolvedModule
        {
            ModuleId = _module.Id,
            NamespaceId = _module.NamespaceId,
            StackId = _module.Namespace.StackId,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            ModuleName = _module.Name,
            NamespaceName = _module.Namespace.Name,
            StackName = "harness",
            RunnerName = _runner.Name,
            SourceRevision = "main",
            SourceUrl = "https://example.com/repo.git",
            SourceSubdirectory = "",
            Engine = "tofu",
            Policies = new List<ResolvedPolicy>()
        };
        db.ApplyJobSagas.Add(new ApplyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = state,
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "stuck-harness",
            DeclaredJson = JsonSerializer.Serialize(declared),
            WaitingSince = waitingSince
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    [Fact]
    public async Task Detects_Stalled_Waits_And_Ignores_Fresh_Or_Pending_Ones()
    {
        var parkedStale = await SeedSaga("ApplyFromPlanWaitingForRunner", DateTime.UtcNow.AddHours(-2));
        var parkedFresh = await SeedSaga("PlanWaitingForRunner", DateTime.UtcNow.AddMinutes(-1));
        var approvalStale = await SeedSaga("WaitingForApproval", DateTime.UtcNow.AddHours(-25));
        var approvalFresh = await SeedSaga("WaitingForApproval", DateTime.UtcNow.AddHours(-1));
        var cancelStale = await SeedSaga("CancellingImmediateKill", DateTime.UtcNow.AddHours(-1));
        var pending = await SeedSaga("PlanPending", DateTime.UtcNow.AddDays(-2));

        var stuck = await CreateService().FindStuckJobsAsync();
        var ids = stuck.Select(s => s.JobId).ToHashSet();

        Assert.Contains(parkedStale, ids);
        Assert.Contains(approvalStale, ids);
        Assert.Contains(cancelStale, ids);
        Assert.DoesNotContain(parkedFresh, ids);
        Assert.DoesNotContain(approvalFresh, ids);
        Assert.DoesNotContain(pending, ids);

        var parked = stuck.Single(s => s.JobId == parkedStale);
        Assert.Equal("ApplyFromPlanWaitingForRunner", parked.State);
        Assert.True(parked.Stalled > TimeSpan.FromMinutes(90));
    }

    [Fact]
    public async Task Entering_A_Cancelling_State_Records_WaitingSince()
    {
        var jobId = await SeedSaga("PlanPending", waitingSince: null);

        await _harness.Bus.Publish(new CancelModuleRequested
        {
            CorrelationId = jobId,
            OrganizationId = _module.OrganizationId,
            CancellationType = CancellationType.ImmediateKill
        });

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            var saga = await db.ApplyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId);
            if (saga is { CurrentState: "CancellingImmediateKill" })
            {
                Assert.NotNull(saga.WaitingSince);
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Saga never entered CancellingImmediateKill");
    }
}
