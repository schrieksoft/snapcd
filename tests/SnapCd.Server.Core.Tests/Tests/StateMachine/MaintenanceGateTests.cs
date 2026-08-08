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
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
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

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// The job-creation gate: with maintenance mode active, a gatekeeping request queues the desired
/// state on the ModuleSaga instead of starting a job, and a dequeue attempt leaves the queue
/// untouched. JobService is deliberately not registered — the gated paths must not resolve it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MaintenanceGateTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private IMaintenanceModeService _maintenance = null!;

    private Guid _moduleId;
    private Guid _organizationId;
    private Module _module = null!;
    private Runner _runner = null!;

    public MaintenanceGateTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            var module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _module = module;
            _runner = db.Runners.First(r => r.Id == module.RunnerId);
            _moduleId = module.Id;
            _organizationId = module.OrganizationId;

            foreach (var instance in new[] { "harness", "park-harness" })
            {
                if (!db.RunnerConnections.Any(rc => rc.RunnerId == _runner.Id && rc.InstanceName == instance))
                    db.RunnerConnections.Add(new RunnerConnection
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = _organizationId,
                        RunnerId = _runner.Id,
                        InstanceName = instance,
                        SignalRConnectionId = instance + "-connection",
                        ServerInstanceId = Guid.NewGuid()
                    });
            }

            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<ModuleStateMachine, ModuleSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
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

        using var scope = _provider.CreateScope();
        _maintenance = new MaintenanceModeService(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IDistributedCache>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MaintenanceModeService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _maintenance.DisableAsync();
        await ResetSaga();
        await _provider.DisposeAsync();
    }

    private async Task ResetSaga()
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
        if (saga != null)
        {
            saga.QueuedDesiredStateHeadline = null;
            saga.QueuedReason = null;
            await db.SaveChangesAsync();
        }
    }

    private async Task EnsureSaga(DesiredStateHeadline? queued = null, QueuedReason? reason = null)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
        if (saga == null)
        {
            saga = new ModuleSaga
            {
                CorrelationId = _moduleId,
                OrganizationId = _organizationId,
                CurrentState = "Gatekeeping"
            };
            db.ModuleSagas.Add(saga);
        }

        saga.CurrentState = "Gatekeeping";
        saga.QueuedDesiredStateHeadline = queued;
        saga.QueuedReason = reason;
        await db.SaveChangesAsync();
    }

    private async Task<ModuleSaga?> WaitForSaga(Func<ModuleSaga, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleSaga? saga = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            saga = await db.ModuleSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
            if (saga != null && predicate(saga)) return saga;
            await Task.Delay(100);
        }
        return saga;
    }

    [Fact]
    public async Task Gatekeeping_Request_During_Maintenance_Queues_Instead_Of_Starting()
    {
        await EnsureSaga();
        await _maintenance.EnableAsync(Guid.NewGuid(), "gate test");

        int jobsBefore;
        await using (var db = _fixture.CreateDbContext())
            jobsBefore = await db.ModuleJobs.CountAsync(j => j.ModuleId == _moduleId);

        await _harness.Bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            SetNewDesiredState = true
        });

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Maintenance);
        Assert.NotNull(saga);
        Assert.Equal(DesiredStateHeadline.Applied, saga!.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.Maintenance, saga.QueuedReason);

        await using (var db = _fixture.CreateDbContext())
            Assert.Equal(jobsBefore, await db.ModuleJobs.CountAsync(j => j.ModuleId == _moduleId));
    }

    [Fact]
    public async Task Dequeue_Attempt_During_Maintenance_Leaves_Queue_Untouched()
    {
        await EnsureSaga(queued: DesiredStateHeadline.Applied, reason: QueuedReason.Maintenance);
        await _maintenance.EnableAsync(Guid.NewGuid(), "gate test");

        await _harness.Bus.Publish(new RunQueueNowRequested
        {
            ModuleId = _moduleId,
            OrganizationId = _organizationId
        });

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var consumed = _harness.Consumed
                .Select<RunQueueNowRequested>(x => ((IReceivedMessage<RunQueueNowRequested>)x).Context.Message.ModuleId == _moduleId)
                .FirstOrDefault();
            if (consumed != null)
            {
                Assert.Null(consumed.Exception);
                var saga = await WaitForSaga(s => true);
                Assert.Equal(DesiredStateHeadline.Applied, saga!.QueuedDesiredStateHeadline);
                Assert.Equal(QueuedReason.Maintenance, saga.QueuedReason);
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("RunQueueNowRequested was never consumed");
    }

    private async Task<Guid> SeedApplyJob(string state)
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            IsCurrent = null
        });
        var declared = new ResolvedModule
        {
            ModuleId = _moduleId,
            NamespaceId = _module.NamespaceId,
            StackId = _module.Namespace.StackId,
            OrganizationId = _organizationId,
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
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "park-harness",
            DeclaredJson = JsonSerializer.Serialize(declared)
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<ApplyJobSaga?> WaitForApplySaga(Guid jobId, Func<ApplyJobSaga, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        ApplyJobSaga? saga = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            saga = await db.ApplyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId);
            if (saga != null && predicate(saga)) return saga;
            await Task.Delay(100);
        }
        return saga;
    }

    [Fact]
    public async Task Dependency_Check_During_Maintenance_Keeps_The_Marker_The_Sweep_Selects_On()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            var seed = await db.ModuleSagas.SingleAsync(s => s.CorrelationId == _moduleId);
            seed.QueuedDesiredStateHeadline = DesiredStateHeadline.Applied;
            seed.QueuedReason = QueuedReason.WaitingOnDependencies;
            await db.SaveChangesAsync();
        }

        await _maintenance.EnableAsync(Guid.NewGuid(), "dependency marker test");

        await _harness.Bus.Publish(new ModuleDependencyCheckRequested
        {
            ModuleId = _moduleId,
            OrganizationId = _organizationId
        });
        await Task.Delay(1500);

        await using (var db = _fixture.CreateDbContext())
        {
            var saga = await db.ModuleSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == _moduleId);
            // The resume sweep finds queued modules by this column; clearing it while the gate
            // refuses to dequeue would strand the module with nothing left to select it.
            Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        }
    }

    [Fact]
    public async Task Step_Completion_During_Maintenance_Parks_At_The_Task_Boundary()
    {
        var jobId = await SeedApplyJob("PlanPending");
        await _maintenance.EnableAsync(Guid.NewGuid(), "park test");

        await _harness.Bus.Publish(new PlanCompleted { CorrelationId = jobId, OrganizationId = _organizationId, TotalChangedCount = 1 });

        var saga = await WaitForApplySaga(jobId, s => s.CurrentState == "ApplyFromPlanWaitingForRunner");
        Assert.NotNull(saga);
        Assert.Equal("ApplyFromPlanWaitingForRunner", saga!.CurrentState);
        Assert.NotNull(saga.WaitingSince);
        // The state the saga parked out of is what the resume branch dispatches from; without it
        // the parked job has nothing to return to.
        Assert.NotNull(saga.PreviousStateBeforeWaiting);

        // The runner IS connected; a reconnect event during the window must not unpark it.
        await _harness.Bus.Publish(new RunnerReconnectedEvent
        {
            OrganizationId = _organizationId,
            RunnerId = _runner.Id,
            InstanceName = "park-harness",
            ServerInstanceId = Guid.NewGuid()
        });
        await Task.Delay(1500);
        saga = await WaitForApplySaga(jobId, s => true);
        Assert.Equal("ApplyFromPlanWaitingForRunner", saga!.CurrentState);

        // Window closes, the resume reconnect event dispatches the next task.
        await _maintenance.DisableAsync();
        await _harness.Bus.Publish(new RunnerReconnectedEvent
        {
            OrganizationId = _organizationId,
            RunnerId = _runner.Id,
            InstanceName = "park-harness",
            ServerInstanceId = Guid.NewGuid()
        });
        saga = await WaitForApplySaga(jobId, s => s.CurrentState == "ApplyFromPlanPending");
        Assert.Equal("ApplyFromPlanPending", saga!.CurrentState);
        Assert.Null(saga.WaitingSince);
    }
}
