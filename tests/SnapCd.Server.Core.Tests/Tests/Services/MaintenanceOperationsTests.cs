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
using Microsoft.AspNetCore.Http;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Middleware;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
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
public class MaintenanceOperationsTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;
    private readonly List<Guid> _seededSagas = [];

    public MaintenanceOperationsTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);

            foreach (var instance in new[] { "ops-park", "ops-bogus" })
            {
                if (!db.RunnerConnections.Any(rc => rc.RunnerId == _runner.Id && rc.InstanceName == instance))
                    db.RunnerConnections.Add(new RunnerConnection
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = _module.OrganizationId,
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
        services.AddSingleton<IMaintenanceModeService, MaintenanceModeService>();
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
        await using (var db = _fixture.CreateDbContext())
        {
            var sagas = await db.ApplyJobSagas.Where(s => _seededSagas.Contains(s.CorrelationId)).ToListAsync();
            db.ApplyJobSagas.RemoveRange(sagas);
            await db.SaveChangesAsync();
        }

        await _provider.DisposeAsync();
    }

    private MaintenanceOperationsService CreateService()
        => new(
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

    /// <summary>
    /// Records whether the window was still open at the moment the sweep published, which is what
    /// determines whether the recovery events survive: the activities that consume them drop the
    /// message outright while a window is open.
    /// </summary>
    private sealed class DisableOrderSpy(IMaintenanceModeService inner) : IMaintenanceModeService
    {
        public bool Disabled { get; private set; }

        public Task DisableAsync()
        {
            Disabled = true;
            return inner.DisableAsync();
        }

        public Task<bool> IsActiveAsync() => Task.FromResult(!Disabled);

        public Task EnableAsync(Guid enabledBy, string? reason) => inner.EnableAsync(enabledBy, reason);
        public Task<MaintenanceModeStatus> GetStatusAsync() => inner.GetStatusAsync();
        public Task<SnapCd.Server.Core.Entities.Definition.MaintenanceMode?> GetAsync() => inner.GetAsync();
        public Task SyncCacheAsync() => inner.SyncCacheAsync();
        public Task AdvanceToAsync(MaintenancePhase phase, IReadOnlyList<MaintenancePhase>? skipped = null) => inner.AdvanceToAsync(phase, skipped);
        public Task RecordPhaseActionAsync(string summary) => inner.RecordPhaseActionAsync(summary);
    }

    [Fact]
    public async Task Resuming_lowers_the_gate_before_the_sweep_publishes()
    {
        var spy = new DisableOrderSpy(_provider.GetRequiredService<IMaintenanceModeService>());
        var operations = new MaintenanceOperationsService(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IBus>(),
            new SnapCd.Server.Core.Services.TransportReconciliationJob(
                _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
                _provider.GetRequiredService<IBus>(),
                Mock.Of<IMessageScheduler>(),
                new SnapCd.Server.Core.Services.QuotaService(Mock.Of<SnapCd.Server.Core.Licensing.Services.IQuotaGatingService>()),
                NullLogger<SnapCd.Server.Core.Services.TransportReconciliationJob>.Instance),
            spy,
            NullLogger<MaintenanceOperationsService>.Instance);

        await operations.RunPhaseActionAsync(MaintenancePhase.Resuming);

        Assert.True(spy.Disabled,
            "the sweep published while the window was still open, so every recovery event it sent would be dropped");
    }

    private async Task<Guid> SeedSaga(string state, string instanceName)
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
            RunnerInstanceName = instanceName,
            DeclaredJson = JsonSerializer.Serialize(declared)
        });
        await db.SaveChangesAsync();
        _seededSagas.Add(jobId);
        return jobId;
    }

    [Fact]
    public async Task Sweep_Wakes_Clean_Parked_Groups_And_Skips_Corrupt_Ones()
    {
        var parked = await SeedSaga("ApplyFromPlanWaitingForRunner", "ops-park");
        var corruptParked = await SeedSaga("PlanWaitingForRunner", "ops-bogus");
        var corrupt = await SeedSaga("TotallyBogusState", "ops-bogus");

        var result = await CreateService().RunResumeSweepAsync();

        Assert.True(await _harness.Published.Any<RunnerReconnectedEvent>(x => x.Context.Message.InstanceName == "ops-park"));
        Assert.False(_harness.Published
            .Select<RunnerReconnectedEvent>(x => ((IPublishedMessage<RunnerReconnectedEvent>)x).Context.Message.InstanceName == "ops-bogus")
            .Any());
        Assert.Contains(result.Warnings, w => w.Contains(corrupt.ToString()) && w.Contains("TotallyBogusState"));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            var saga = await db.ApplyJobSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == parked);
            if (saga.CurrentState == "ApplyFromPlanPending") return;
            await Task.Delay(100);
        }

        throw new TimeoutException("Parked saga was never woken");
    }

    [Fact]
    public async Task Sweep_Redrives_Queued_Modules()
    {
        DesiredStateHeadline? originalQueued;
        QueuedReason? originalReason;
        await using (var db = _fixture.CreateDbContext())
        {
            var moduleSaga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _module.Id);
            if (moduleSaga == null)
            {
                moduleSaga = new ModuleSaga { CorrelationId = _module.Id, OrganizationId = _module.OrganizationId, CurrentState = "Gatekeeping" };
                db.ModuleSagas.Add(moduleSaga);
            }

            originalQueued = moduleSaga.QueuedDesiredStateHeadline;
            originalReason = moduleSaga.QueuedReason;
            moduleSaga.QueuedDesiredStateHeadline = DesiredStateHeadline.Applied;
            moduleSaga.QueuedReason = QueuedReason.Maintenance;
            await db.SaveChangesAsync();
        }

        try
        {
            var result = await CreateService().RunResumeSweepAsync();
            Assert.True(result.ModulesRequeued >= 1);
            Assert.True(await _harness.Published.Any<ModuleDependencyCheckRequested>(x => x.Context.Message.ModuleId == _module.Id));
        }
        finally
        {
            await using var db = _fixture.CreateDbContext();
            var moduleSaga = await db.ModuleSagas.SingleAsync(s => s.CorrelationId == _module.Id);
            moduleSaga.QueuedDesiredStateHeadline = originalQueued;
            moduleSaga.QueuedReason = originalReason;
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(MaintenancePhase.Draining, false)]
    [InlineData(MaintenancePhase.ReadyForMaintenance, true)]
    [InlineData(MaintenancePhase.Reconciling, true)]
    [InlineData(MaintenancePhase.Resuming, false)]
    public void Closing_Needs_Recovery_Only_Where_Work_Is_Parked(MaintenancePhase phase, bool needsRecovery)
        => Assert.Equal(needsRecovery, MaintenanceOperationsService.ClosingNeedsRecovery(phase));

    [Fact]
    public async Task CancelAll_Cancels_Pending_And_Skips_Approval_Waits()
    {
        var pending = await SeedSaga("PlanPending", "ops-cancel");
        var approval = await SeedSaga("WaitingForApproval", "ops-cancel");

        var result = await CreateService().CancelAllJobsAsync(CancellationType.ImmediateGraceful);

        Assert.True(await _harness.Published.Any<CancelModuleRequested>(x => x.Context.Message.CorrelationId == pending));
        Assert.False(_harness.Published
            .Select<CancelModuleRequested>(x => ((IPublishedMessage<CancelModuleRequested>)x).Context.Message.CorrelationId == approval)
            .Any());
        Assert.Contains(result.Skipped, s => s.Contains(approval.ToString()));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            var saga = await db.ApplyJobSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == pending);
            if (saga.CurrentState == "CancellingImmediateGraceful") return;
            await Task.Delay(100);
        }

        throw new TimeoutException("Pending saga never entered cancellation");
    }

    [Theory]
    [InlineData("/api/organization", true)]
    [InlineData("/api/some/other/thing", true)]
    [InlineData("/api", true)]
    [InlineData("/api/state/store/file", false)]
    [InlineData("/api/6ce7c1a4-0000-0000-0000-000000000001/state/store/file", false)]
    [InlineData("/health", false)]
    [InlineData("/AdminCenter", false)]
    [InlineData("/_blazor", false)]
    [InlineData("/runnerhub", false)]
    [InlineData("/agenthub", false)]
    public void Middleware_Refuses_Only_NonState_Api_Routes(string path, bool refused)
        => Assert.Equal(refused, MaintenanceModeMiddleware.ShouldRefuse(new PathString(path)));
}
