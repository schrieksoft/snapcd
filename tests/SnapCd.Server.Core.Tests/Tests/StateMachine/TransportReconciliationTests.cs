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
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;
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
/// Heartbeat re-arm semantics: a tick carrying the scheduling token of a superseded cycle is
/// dropped, a tick carrying the current token is accepted, and a bare tick (as published by
/// TransportReconciliationJob) is always accepted — so the reconciler can republish ticks
/// unconditionally without forking a second heartbeat loop on sagas whose cycle is still live.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransportReconciliationTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    private Module _module = null!;
    private Runner _runner = null!;

    public TransportReconciliationTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
            _runner = db.Runners.First(r => r.Id == _module.RunnerId);

            if (!db.RunnerConnections.Any(rc => rc.RunnerId == _runner.Id && rc.InstanceName == "harness"))
            {
                db.RunnerConnections.Add(new RunnerConnection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _module.OrganizationId,
                    RunnerId = _runner.Id,
                    InstanceName = "harness",
                    SignalRConnectionId = "harness-connection",
                    ServerInstanceId = Guid.NewGuid()
                });
                await db.SaveChangesAsync();
            }
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_module.OrganizationId]));
        services.AddDistributedMemoryCache();
        services.AddSingleton<SnapCd.Server.Core.Services.MaintenanceMode.IMaintenanceModeService, SnapCd.Server.Core.Services.MaintenanceMode.MaintenanceModeService>();
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
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
            x.AddSagaStateMachine<ModuleModifiedStateMachine, ModuleModifiedSaga>()
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

    private string DeclaredJson()
    {
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
        return JsonSerializer.Serialize(declared);
    }

    private async Task<Guid> SeedApplyJob(string state, Guid? scheduleTokenId = null, DateTime? waitingSince = null, int? approvalTimeoutMinutes = null)
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

        db.ApplyJobSagas.Add(new ApplyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = state,
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "harness",
            DeclaredJson = DeclaredJson(),
            HeartbeatScheduleTokenId = scheduleTokenId,
            WaitingSince = waitingSince,
            ApprovalTimeoutMinutes = approvalTimeoutMinutes
        });

        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<Guid> SeedDestroySaga()
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();
        db.DestroyJobSagas.Add(new DestroyJobSaga
        {
            CorrelationId = jobId,
            CurrentState = "PlanPending",
            ModuleId = _module.Id,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            RunnerName = _runner.Name,
            RunnerInstanceName = "harness",
            DeclaredJson = DeclaredJson()
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task AwaitTickConsumed(Guid jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var match = _harness.Consumed
                .Select<HeartbeatScheduled>(x => ((IReceivedMessage<HeartbeatScheduled>)x).Context.Message.CorrelationId == jobId)
                .FirstOrDefault();
            if (match != null)
            {
                Assert.Null(match.Exception);
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Tick for {jobId} was never consumed");
    }

    private bool RequestPublishedFor(Guid jobId)
        => _harness.Published
            .Select<HeartbeatRequested>(x => ((IPublishedMessage<HeartbeatRequested>)x).Context.Message.CorrelationId == jobId)
            .Any();

    [Fact]
    public async Task Bare_Tick_Restarts_Cycle_Despite_Stale_Token()
    {
        // Post-switch shape: the saga still holds a token that references the dead transport.
        var jobId = await SeedApplyJob("PlanPending", scheduleTokenId: Guid.NewGuid());

        await _harness.Bus.Publish(new HeartbeatScheduled { CorrelationId = jobId, OrganizationId = _module.OrganizationId });

        Assert.True(await _harness.Published.Any<HeartbeatRequested>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task Tick_With_Current_Token_Is_Accepted()
    {
        var token = Guid.NewGuid();
        var jobId = await SeedApplyJob("PlanPending", scheduleTokenId: token);

        await _harness.Bus.Publish(
            new HeartbeatScheduled { CorrelationId = jobId, OrganizationId = _module.OrganizationId },
            c => c.Headers.Set(MessageHeaders.SchedulingTokenId, token));

        Assert.True(await _harness.Published.Any<HeartbeatRequested>(x => x.Context.Message.CorrelationId == jobId));
    }

    [Fact]
    public async Task Tick_From_Superseded_Cycle_Is_Dropped()
    {
        var jobId = await SeedApplyJob("PlanPending", scheduleTokenId: Guid.NewGuid());

        await _harness.Bus.Publish(
            new HeartbeatScheduled { CorrelationId = jobId, OrganizationId = _module.OrganizationId },
            c => c.Headers.Set(MessageHeaders.SchedulingTokenId, Guid.NewGuid()));

        await AwaitTickConsumed(jobId);
        Assert.False(RequestPublishedFor(jobId));
    }

    [Fact]
    public async Task Reconciler_Rearms_Pending_Sagas_And_Resting_States_Ignore_It()
    {
        var pendingJob = await SeedApplyJob("PlanPending", scheduleTokenId: Guid.NewGuid());
        var approvalJob = await SeedApplyJob("WaitingForApproval");
        var parkedJob = await SeedApplyJob("ApplyFromPlanWaitingForRunner");
        var destroyJob = await SeedDestroySaga();

        await CreateJob(CapturingScheduler().Mock.Object).ExecuteJob();

        // Every live saga row got a tick, both types.
        Assert.True(await _harness.Published.Any<HeartbeatScheduled>(x => x.Context.Message.CorrelationId == pendingJob));
        Assert.True(await _harness.Published.Any<HeartbeatScheduled>(x => x.Context.Message.CorrelationId == destroyJob));

        // The pending saga restarted its cycle; resting states ignored theirs.
        Assert.True(await _harness.Published.Any<HeartbeatRequested>(x => x.Context.Message.CorrelationId == pendingJob));
        await AwaitTickConsumed(approvalJob);
        await AwaitTickConsumed(parkedJob);
        Assert.False(RequestPublishedFor(approvalJob));
        Assert.False(RequestPublishedFor(parkedJob));
    }

    private sealed record SchedulerCapture(
        Mock<IMessageScheduler> Mock,
        List<(DateTime Time, ApprovalTimeoutReceived Message)> Approvals,
        List<(DateTime Time, DriftCheckScheduled Message, Guid Token)> Drifts,
        List<(DateTime Time, MissionRunDeadlineCheck Message)> Missions);

    private TransportReconciliationJob CreateJob(IMessageScheduler scheduler, ILogger<TransportReconciliationJob>? logger = null)
        => new(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IBus>(),
            scheduler,
            new QuotaService(Mock.Of<IQuotaGatingService>()),
            logger ?? NullLogger<TransportReconciliationJob>.Instance);

    private sealed class CollectingLogger : ILogger<TransportReconciliationJob>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    private static SchedulerCapture CapturingScheduler()
    {
        var approvals = new List<(DateTime, ApprovalTimeoutReceived)>();
        var drifts = new List<(DateTime, DriftCheckScheduled, Guid)>();
        var missions = new List<(DateTime, MissionRunDeadlineCheck)>();
        var scheduler = new Mock<IMessageScheduler>();
        scheduler
            .Setup(s => s.SchedulePublish(It.IsAny<DateTime>(), It.IsAny<ApprovalTimeoutReceived>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, ApprovalTimeoutReceived, CancellationToken>((t, m, _) => approvals.Add((t, m)))
            .ReturnsAsync((ScheduledMessage<ApprovalTimeoutReceived>)null!);
        scheduler
            .Setup(s => s.SchedulePublish(It.IsAny<DateTime>(), It.IsAny<DriftCheckScheduled>(), It.IsAny<CancellationToken>()))
            .Returns<DateTime, DriftCheckScheduled, CancellationToken>((t, m, _) =>
            {
                var token = Guid.NewGuid();
                drifts.Add((t, m, token));
                return Task.FromResult(Mock.Of<ScheduledMessage<DriftCheckScheduled>>(x => x.TokenId == token));
            });
        scheduler
            .Setup(s => s.SchedulePublish(It.IsAny<DateTime>(), It.IsAny<MissionRunDeadlineCheck>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, MissionRunDeadlineCheck, CancellationToken>((t, m, _) => missions.Add((t, m)))
            .ReturnsAsync((ScheduledMessage<MissionRunDeadlineCheck>)null!);
        return new SchedulerCapture(scheduler, approvals, drifts, missions);
    }

    [Fact]
    public async Task Reconciler_Reschedules_Future_Approval_Deadline()
    {
        var waitingSince = DateTime.UtcNow.AddMinutes(-2);
        var jobId = await SeedApplyJob("WaitingForApproval", waitingSince: waitingSince, approvalTimeoutMinutes: 30);

        var capture = CapturingScheduler();
        await CreateJob(capture.Mock.Object).ExecuteJob();

        var entry = capture.Approvals.Single(c => c.Message.CorrelationId == jobId);
        var expected = waitingSince.AddMinutes(30);
        Assert.True(Math.Abs((entry.Time - expected).TotalSeconds) < 5,
            $"Expected deadline ~{expected:O}, scheduled at {entry.Time:O}");
    }

    [Fact]
    public async Task Reconciler_Applies_Grace_To_Lapsed_Approval_Deadline()
    {
        var jobId = await SeedApplyJob("WaitingForApproval", waitingSince: DateTime.UtcNow.AddHours(-2), approvalTimeoutMinutes: 5);

        var capture = CapturingScheduler();
        var before = DateTime.UtcNow;
        await CreateJob(capture.Mock.Object).ExecuteJob();

        var entry = capture.Approvals.Single(c => c.Message.CorrelationId == jobId);
        Assert.True(entry.Time > before.AddMinutes(50) && entry.Time < DateTime.UtcNow.AddMinutes(70),
            $"Expected grace-shifted deadline ~1h out, scheduled at {entry.Time:O}");
    }

    [Fact]
    public async Task Reconciler_Skips_Approval_Sagas_Without_Timeout()
    {
        var jobId = await SeedApplyJob("WaitingForApproval", waitingSince: DateTime.UtcNow);

        var capture = CapturingScheduler();
        await CreateJob(capture.Mock.Object).ExecuteJob();

        Assert.DoesNotContain(capture.Approvals, c => c.Message.CorrelationId == jobId);
    }

    private async Task<(bool? Enabled, int? Interval, Guid? Token)> ArmModuleDrift(bool enabled, int? interval, Guid token)
    {
        await using var db = _fixture.CreateDbContext();
        var module = db.Modules.First(m => m.Id == _module.Id);
        var original = (module.DriftCheckEnabled, module.DriftCheckIntervalMinutes, (Guid?)null);
        module.DriftCheckEnabled = enabled;
        module.DriftCheckIntervalMinutes = interval;

        var saga = db.ModuleSagas.SingleOrDefault(s => s.CorrelationId == _module.Id);
        if (saga == null)
        {
            saga = new ModuleSaga
            {
                CorrelationId = _module.Id,
                OrganizationId = _module.OrganizationId,
                CurrentState = "Gatekeeping"
            };
            db.ModuleSagas.Add(saga);
        }

        original = (original.DriftCheckEnabled, original.DriftCheckIntervalMinutes, saga.DriftCheckScheduleTokenId);
        saga.DriftCheckScheduleTokenId = token;
        await db.SaveChangesAsync();
        return original;
    }

    private async Task RestoreModuleDrift((bool? Enabled, int? Interval, Guid? Token) original)
    {
        await using var db = _fixture.CreateDbContext();
        var module = db.Modules.First(m => m.Id == _module.Id);
        module.DriftCheckEnabled = original.Enabled;
        module.DriftCheckIntervalMinutes = original.Interval;
        var saga = db.ModuleSagas.Single(s => s.CorrelationId == _module.Id);
        saga.DriftCheckScheduleTokenId = original.Token;
        await db.SaveChangesAsync();
    }

    private async Task<Guid?> GetDriftToken()
    {
        await using var db = _fixture.CreateDbContext();
        return (await db.ModuleSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == _module.Id)).DriftCheckScheduleTokenId;
    }

    [Fact]
    public async Task Reconciler_Reschedules_Armed_Drift_Check_And_Writes_Back_Token()
    {
        var original = await ArmModuleDrift(enabled: true, interval: 800, token: Guid.NewGuid());
        try
        {
            var capture = CapturingScheduler();
            var before = DateTime.UtcNow;
            await CreateJob(capture.Mock.Object).ExecuteJob();

            var entry = capture.Drifts.Single(d => d.Message.ModuleId == _module.Id);
            var expected = before.AddMinutes(800);
            Assert.True(Math.Abs((entry.Time - expected).TotalMinutes) < 2,
                $"Expected drift check ~{expected:O}, scheduled at {entry.Time:O}");
            Assert.Equal(entry.Token, await GetDriftToken());
        }
        finally
        {
            await RestoreModuleDrift(original);
        }
    }

    [Fact]
    public async Task Reconciler_Clears_Drift_Token_When_Disabled()
    {
        var original = await ArmModuleDrift(enabled: false, interval: null, token: Guid.NewGuid());
        try
        {
            var capture = CapturingScheduler();
            await CreateJob(capture.Mock.Object).ExecuteJob();

            Assert.DoesNotContain(capture.Drifts, d => d.Message.ModuleId == _module.Id);
            Assert.Null(await GetDriftToken());
        }
        finally
        {
            await RestoreModuleDrift(original);
        }
    }

    [Fact]
    public async Task Drift_Tick_From_Superseded_Schedule_Is_Dropped_And_Bare_Tick_Accepted()
    {
        var original = await ArmModuleDrift(enabled: true, interval: 800, token: Guid.NewGuid());
        try
        {
            // Superseded token: consumed, but no gatekeeping request is published.
            await _harness.Bus.Publish(
                new DriftCheckScheduled { ModuleId = _module.Id, OrganizationId = _module.OrganizationId },
                c => c.Headers.Set(MessageHeaders.SchedulingTokenId, Guid.NewGuid()));
            await AwaitDriftTickConsumed(1);
            Assert.False(_harness.Published
                .Select<GatekeepingJobRequested>(x => ((IPublishedMessage<GatekeepingJobRequested>)x).Context.Message.ModuleId == _module.Id)
                .Any());

            // Bare tick (the reconciler's shape): accepted, gatekeeping request published.
            await _harness.Bus.Publish(new DriftCheckScheduled { ModuleId = _module.Id, OrganizationId = _module.OrganizationId });
            await AwaitDriftTickConsumed(2);
            Assert.True(await _harness.Published.Any<GatekeepingJobRequested>(x => x.Context.Message.ModuleId == _module.Id));
        }
        finally
        {
            await RestoreModuleDrift(original);
        }
    }

    private async Task AwaitDriftTickConsumed(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var consumed = _harness.Consumed
                .Select<DriftCheckScheduled>(x => ((IReceivedMessage<DriftCheckScheduled>)x).Context.Message.ModuleId == _module.Id)
                .ToList();
            if (consumed.Count >= count)
            {
                Assert.All(consumed, c => Assert.Null(c.Exception));
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Drift tick was never consumed");
    }

    private async Task SetModifiedSagaState(string state)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = db.Set<ModuleModifiedSaga>().SingleOrDefault(s => s.CorrelationId == _module.Id);
        if (saga == null)
        {
            saga = new ModuleModifiedSaga
            {
                CorrelationId = _module.Id,
                OrganizationId = _module.OrganizationId,
                CurrentState = state
            };
            db.Set<ModuleModifiedSaga>().Add(saga);
        }
        else
        {
            saga.CurrentState = state;
        }

        await db.SaveChangesAsync();
    }

    private async Task<string?> WaitForModifiedSagaState(Func<string?, bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        string? state = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var db = _fixture.CreateDbContext();
            state = (await db.Set<ModuleModifiedSaga>().AsNoTracking().SingleOrDefaultAsync(s => s.CorrelationId == _module.Id))?.CurrentState;
            if (predicate(state)) return state;
            await Task.Delay(100);
        }
        return state;
    }

    [Fact]
    public async Task Reconciler_Rearms_Parked_Debounce_Which_Then_Flushes()
    {
        await SetModifiedSagaState("WaitingForMoreEvents");

        await CreateJob(CapturingScheduler().Mock.Object).ExecuteJob();

        // The republished trigger re-arms the 5s debounce; the machine's own tick then flushes it.
        Assert.Equal("Idle", await WaitForModifiedSagaState(s => s == "Idle", TimeSpan.FromSeconds(15)));
        Assert.True(_harness.Published
            .Select<GatekeepingJobRequested>(x => ((IPublishedMessage<GatekeepingJobRequested>)x).Context.Message.ModuleId == _module.Id)
            .Any());
    }

    [Fact]
    public async Task Late_Debounce_Tick_In_Idle_Is_Ignored()
    {
        await SetModifiedSagaState("Idle");
        await _harness.Bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = _module.Id, OrganizationId = _module.OrganizationId });
        Assert.Equal("WaitingForMoreEvents", await WaitForModifiedSagaState(s => s == "WaitingForMoreEvents", TimeSpan.FromSeconds(10)));

        // The armed tick now lands in Idle, as it would after a reconciler flush raced it.
        await SetModifiedSagaState("Idle");

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var consumed = _harness.Consumed
                .Select<ModuleModifiedWaitForNextTimeoutScheduled>(x => true)
                .ToList();
            if (consumed.Count >= 1)
            {
                Assert.All(consumed, c => Assert.Null(c.Exception));
                Assert.Equal("Idle", await WaitForModifiedSagaState(s => s == "Idle", TimeSpan.FromSeconds(2)));
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Debounce tick was never consumed");
    }

    private async Task<Guid> SeedMissionRun(MissionStatus status, DateTime deadlineAt)
    {
        await using var db = _fixture.CreateDbContext();

        var jobId = Guid.NewGuid();
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

        var missionId = Guid.NewGuid();
        db.ModuleJobMissions.Add(new ModuleJobMission
        {
            Id = missionId,
            OrganizationId = _module.OrganizationId,
            ModuleJobId = jobId,
            MissionId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            MissionType = MissionType.AutoDiagnose,
            Status = status
        });

        var runId = Guid.NewGuid();
        db.ModuleJobMissionRuns.Add(new ModuleJobMissionRun
        {
            Id = runId,
            OrganizationId = _module.OrganizationId,
            ModuleJobMissionId = missionId,
            ModuleJobId = jobId,
            MissionType = MissionType.AutoDiagnose,
            AgentId = Guid.NewGuid(),
            InvocationId = Guid.NewGuid(),
            AttemptNumber = 1,
            Status = status,
            DeadlineAt = deadlineAt
        });

        await db.SaveChangesAsync();
        return runId;
    }

    private async Task FinalizeMissionRun(Guid runId)
    {
        await using var db = _fixture.CreateDbContext();
        var run = db.ModuleJobMissionRuns.Single(r => r.Id == runId);
        run.Status = MissionStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Reconciler_Reschedules_Future_Mission_Deadline()
    {
        var deadlineAt = DateTime.UtcNow.AddMinutes(30);
        var runId = await SeedMissionRun(MissionStatus.Running, deadlineAt);
        try
        {
            var capture = CapturingScheduler();
            await CreateJob(capture.Mock.Object).ExecuteJob();

            var entry = capture.Missions.Single(m => m.Message.RunId == runId);
            Assert.True(Math.Abs((entry.Time - deadlineAt).TotalSeconds) < 5,
                $"Expected deadline ~{deadlineAt:O}, scheduled at {entry.Time:O}");
        }
        finally
        {
            await FinalizeMissionRun(runId);
        }
    }

    [Fact]
    public async Task Reconciler_Applies_Reconnect_Grace_To_Lapsed_Mission_Deadline()
    {
        var runId = await SeedMissionRun(MissionStatus.AwaitingReconnect, DateTime.UtcNow.AddMinutes(-10));
        try
        {
            var capture = CapturingScheduler();
            var before = DateTime.UtcNow;
            await CreateJob(capture.Mock.Object).ExecuteJob();

            var entry = capture.Missions.Single(m => m.Message.RunId == runId);
            Assert.True(entry.Time > before.AddMinutes(4) && entry.Time < DateTime.UtcNow.AddMinutes(6),
                $"Expected grace-shifted check ~5min out, scheduled at {entry.Time:O}");
        }
        finally
        {
            await FinalizeMissionRun(runId);
        }
    }

    [Fact]
    public async Task Reconciler_Skips_Terminal_Mission_Runs()
    {
        var runId = await SeedMissionRun(MissionStatus.Succeeded, DateTime.UtcNow.AddMinutes(30));

        var capture = CapturingScheduler();
        await CreateJob(capture.Mock.Object).ExecuteJob();

        Assert.DoesNotContain(capture.Missions, m => m.Message.RunId == runId);
    }

    [Fact]
    public async Task Reconciler_Redrives_Stranded_Runner_Selection()
    {
        var jobId = await SeedApplyJob("SelectRunnerInstancePending");

        await CreateJob(CapturingScheduler().Mock.Object).ExecuteJob();

        Assert.True(await _harness.Published.Any<SelectRunnerInstanceRequested>(x =>
            x.Context.Message.CorrelationId == jobId && x.Context.Message.Declared.ModuleId == _module.Id));
    }

    [Fact]
    public async Task Duplicate_SelectRunnerInstance_Response_Is_Ignored()
    {
        var jobId = await SeedApplyJob("GetDefinitiveRevisionPending");

        await _harness.Bus.Publish(new SelectRunnerInstanceCompleted
        {
            CorrelationId = jobId,
            OrganizationId = _module.OrganizationId,
            RunnerInstanceName = "harness"
        });

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var consumed = _harness.Consumed
                .Select<SelectRunnerInstanceCompleted>(x => ((IReceivedMessage<SelectRunnerInstanceCompleted>)x).Context.Message.CorrelationId == jobId)
                .FirstOrDefault();
            if (consumed != null)
            {
                Assert.Null(consumed.Exception);
                await using var db = _fixture.CreateDbContext();
                var saga = await db.ApplyJobSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == jobId);
                Assert.Equal("GetDefinitiveRevisionPending", saga.CurrentState);
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Duplicate response was never consumed");
    }

    [Fact]
    public async Task Reconciler_Flags_Stranded_Cancellations()
    {
        var jobId = await SeedApplyJob("CancellingImmediateKill");

        var logger = new CollectingLogger();
        await CreateJob(CapturingScheduler().Mock.Object, logger).ExecuteJob();

        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Warning && e.Message.Contains(jobId.ToString()) && e.Message.Contains("CancellingImmediateKill"));
    }
}
