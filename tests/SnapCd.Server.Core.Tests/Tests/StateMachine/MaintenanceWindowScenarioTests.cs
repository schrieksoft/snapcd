// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using Moq;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Tests.Infrastructure.Fakes;
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
/// The maintenance window scenario matrix, driven by a fake runner and a fake agent against the
/// real sagas, consumers, repositories and gates. The hubs themselves only authorize and
/// delegate, so the fakes drive the handler layer under the same caller scopes the hub filters
/// apply — which is exactly what the window's exemptions turn on.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MaintenanceWindowScenarioTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private IMaintenanceModeService _maintenance = null!;
    private FakeRunner _runner = null!;
    private FakeAgent _agent = null!;

    private Module _module = null!;
    private Runner _runnerEntity = null!;
    private readonly List<Guid> _seededSagas = [];

    public MaintenanceWindowScenarioTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runnerEntity = db.Runners.First(r => r.Id == _module.RunnerId);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]));
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
        services.AddSingleton<IMaintenanceModeService, MaintenanceModeService>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<ApplyMachine, ApplyJobSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
            x.AddSagaStateMachine<ModuleStateMachine, ModuleSaga>()
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

        var factory = _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        _maintenance = _provider.GetRequiredService<IMaintenanceModeService>();
        _runner = new FakeRunner(_provider.GetRequiredService<IBus>(), factory, _module.OrganizationId, _runnerEntity.Id, "scenario-runner");
        _agent = new FakeAgent(factory, _module.OrganizationId, Guid.NewGuid());
        await _runner.ConnectAsync();
    }

    public async Task DisposeAsync()
    {
        await _maintenance.DisableAsync();
        MaintenanceGate.Reset();
        await using (var db = _fixture.CreateDbContext())
        {
            var sagas = await db.ApplyJobSagas.Where(s => _seededSagas.Contains(s.CorrelationId)).ToListAsync();
            db.ApplyJobSagas.RemoveRange(sagas);
            var moduleSaga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _module.Id);
            if (moduleSaga != null)
            {
                moduleSaga.QueuedDesiredStateHeadline = null;
                moduleSaga.QueuedReason = null;
            }

            await db.SaveChangesAsync();
        }

        await _runner.DisconnectAsync();
        await _provider.DisposeAsync();
    }

    private async Task<Guid> SeedJob(string state)
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
            RunnerId = _runnerEntity.Id,
            ModuleName = _module.Name,
            NamespaceName = _module.Namespace.Name,
            StackName = "harness",
            RunnerName = _runnerEntity.Name,
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
            RunnerId = _runnerEntity.Id,
            RunnerName = _runnerEntity.Name,
            RunnerInstanceName = _runner.InstanceName,
            DeclaredJson = JsonSerializer.Serialize(declared)
        });
        await db.SaveChangesAsync();
        _seededSagas.Add(jobId);
        return jobId;
    }

    private async Task<string?> WaitForState(Guid jobId, Func<string?, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        string? state = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            state = (await db.ApplyJobSagas.AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == jobId))?.CurrentState;
            if (predicate(state)) return state;
            await Task.Delay(100);
        }

        return state;
    }

    // Scenario 1: a window opening mid-plan lets the current task finish, then parks the job.
    [Fact]
    public async Task Window_During_Plan_Parks_At_The_Task_Boundary_And_Resumes_After()
    {
        var jobId = await SeedJob("PlanPending");
        await _maintenance.EnableAsync(Guid.NewGuid(), "scenario 1");

        await _runner.CompletePlanAsync(jobId);

        Assert.Equal("ApplyFromPlanWaitingForRunner", await WaitForState(jobId, s => s == "ApplyFromPlanWaitingForRunner"));

        await _maintenance.DisableAsync();
        var operations = new MaintenanceOperationsService(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IBus>(),
            new SnapCd.Server.Core.Services.TransportReconciliationJob(
                _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
                _provider.GetRequiredService<IBus>(),
                Mock.Of<IMessageScheduler>(),
                new SnapCd.Server.Core.Services.QuotaService(Mock.Of<SnapCd.Server.Core.Licensing.Services.IQuotaGatingService>()),
                NullLogger<SnapCd.Server.Core.Services.TransportReconciliationJob>.Instance),
            _provider.GetRequiredService<IMaintenanceModeService>(),
            NullLogger<MaintenanceOperationsService>.Instance);
        await operations.RunResumeSweepAsync();

        Assert.Equal("ApplyFromPlanPending", await WaitForState(jobId, s => s == "ApplyFromPlanPending"));
    }

    // Scenario 2: runner writes stay exempt while human writes are refused.
    [Fact]
    public async Task Runner_Writes_Pass_While_Human_Writes_Are_Refused()
    {
        var jobId = await SeedJob("PlanPending");
        MaintenanceGate.Initialize(_maintenance);
        try
        {
            await _maintenance.EnableAsync(Guid.NewGuid(), "scenario 2");

            // The runner's progress report writes RunnerConnectionJobs through the gate.
            await _runner.ReportRunningTaskAsync(jobId, "Plan");

            await using var db = _fixture.CreateDbContext();
            var repo = new ModuleJobRepository(
                db,
                new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]),
                _provider.GetRequiredService<IBus>(),
                Options.Create(new ModuleJobRepositorySettings()));
            var job = await db.ModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
            job.PlanTotalChangedCount = 3;
            await Assert.ThrowsAsync<MaintenanceModeException>(() => repo.ExecuteUpdate(job));
        }
        finally
        {
            MaintenanceGate.Reset();
            await _maintenance.DisableAsync();
        }
    }

    // Scenario 3: new work requested during a window queues instead of starting.
    [Fact]
    public async Task New_Work_During_A_Window_Queues_And_Resumes_On_Close()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            var moduleSaga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _module.Id);
            if (moduleSaga == null)
            {
                db.ModuleSagas.Add(new ModuleSaga { CorrelationId = _module.Id, OrganizationId = _module.OrganizationId, CurrentState = "Gatekeeping" });
            }
            else
            {
                moduleSaga.CurrentState = "Gatekeeping";
                moduleSaga.QueuedDesiredStateHeadline = null;
                moduleSaga.QueuedReason = null;
            }

            await db.SaveChangesAsync();
        }

        await _maintenance.EnableAsync(Guid.NewGuid(), "scenario 3");

        await _harness.Bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            SetNewDesiredState = true
        });

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            var saga = await db.ModuleSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == _module.Id);
            if (saga.QueuedReason == QueuedReason.Maintenance) return;
            await Task.Delay(100);
        }

        throw new TimeoutException("Module was never queued for maintenance");
    }

    // Scenario 4: an agent's mission keeps its deadline alive through a window, and the drain
    // board counts it as outstanding work.
    [Fact]
    public async Task Agent_Mission_Survives_A_Window_And_Counts_As_Outstanding()
    {
        var jobId = await SeedJob("ApplyFromPlanPending");
        var (_, runId, invocationId) = await _agent.StartMissionAsync(jobId, MissionType.AutoDiagnose);

        await _maintenance.EnableAsync(Guid.NewGuid(), "scenario 4");

        var factory = _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        var probe = new QuiescenceProbeService(factory, new StuckJobDetectionService(factory, Options.Create(new StuckJobDetectionSettings())));

        var during = await probe.GetDrainStatusAsync();
        Assert.True(during.ActiveMissions >= 1);
        Assert.False(during.IsDatabaseQuiet);

        // The agent heartbeats through the window; its deadline moves forward.
        DateTime deadlineBefore;
        await using (var db = _fixture.CreateDbContext())
            deadlineBefore = (await db.ModuleJobMissionRuns.AsNoTracking().SingleAsync(r => r.Id == runId)).DeadlineAt;

        await Task.Delay(1100);
        await _agent.HeartbeatAsync(invocationId);

        await using (var db = _fixture.CreateDbContext())
        {
            var run = await db.ModuleJobMissionRuns.AsNoTracking().SingleAsync(r => r.Id == runId);
            Assert.True(run.DeadlineAt > deadlineBefore, "heartbeat did not extend the mission deadline");
            Assert.Equal(MissionStatus.Running, run.Status);
        }

        // Disconnect parks it; reconnecting resumes it — the mission analogue of runner parking.
        await _agent.DisconnectAsync(invocationId);
        await using (var db = _fixture.CreateDbContext())
            Assert.Equal(MissionStatus.AwaitingReconnect, (await db.ModuleJobMissionRuns.AsNoTracking().SingleAsync(r => r.Id == runId)).Status);

        await _agent.HeartbeatAsync(invocationId);
        await using (var db = _fixture.CreateDbContext())
            Assert.Equal(MissionStatus.Running, (await db.ModuleJobMissionRuns.AsNoTracking().SingleAsync(r => r.Id == runId)).Status);

        await _agent.CompleteMissionAsync(invocationId);
        var after = await probe.GetDrainStatusAsync();
        Assert.Equal(during.ActiveMissions - 1, after.ActiveMissions);
    }
}
