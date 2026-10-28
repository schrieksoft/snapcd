// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using System.Collections.Concurrent;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.Crud.Jobs;
using SnapCd.Server.Core.Services.Nonsecured.MaintenanceMode;
using SnapCd.Server.Core.Services.Nonsecured.ModuleHold;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Services.Utils.PrincipalProvider;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// The hold gate on ModuleSaga: a held Module parks triggered and queued work, a hold check reschedules
/// itself while the hold lasts, and the queue is released through the dependency check once it lapses.
/// Hold checks are captured from a mock scheduler and delivered by the test, against a manual clock.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleHoldGateTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly ManualClock _clock = new(DateTimeOffset.UtcNow);
    private readonly ConcurrentBag<Guid> _applied = [];
    private readonly ConcurrentQueue<(DateTime Time, ModuleHoldCheck Message)> _scheduled = new();
    private readonly List<Guid> _seededJobs = [];
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Guid _moduleId;
    private Guid _organizationId;

    public ModuleHoldGateTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var scheduler = new Mock<IMessageScheduler>();
        scheduler
            .Setup(s => s.SchedulePublish(It.IsAny<DateTime>(), It.IsAny<ModuleHoldCheck>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, ModuleHoldCheck, CancellationToken>((t, m, _) => _scheduled.Enqueue((t, m)))
            .ReturnsAsync((ScheduledMessage<ModuleHoldCheck>)null!);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddSingleton<TimeProvider>(_clock);
        services.Configure<ModuleHoldSettings>(_ => { });
        services.AddScoped<ModuleHoldStore>();
        services.AddScoped<ModuleHoldGate>();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext());
        services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
        services.AddScoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, [_organizationId]));
        services.Configure<ModuleJobRepositorySettings>(_ => { });
        services.AddScoped<ModuleJobRepository>();
        services.AddScoped<ModuleJobRepositoryFactory>();
        services.AddSingleton(_fixture.CreateMockQuotaService());
        services.AddScoped<JobService>(sp => new StubJobService(sp.GetRequiredService<SnapCdDbContext>(), _applied));
        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<ModuleStateMachine, ModuleSaga>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<SnapCdDbContext>();
                });
        });
        services.AddSingleton(scheduler.Object);

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();

        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMaintenanceModeService>().DisableAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
            if (saga != null)
            {
                saga.QueuedDesiredStateHeadline = null;
                saga.QueuedReason = null;
            }

            db.ModuleJobs.RemoveRange(db.ModuleJobs.Where(j => _seededJobs.Contains(j.Id)));
            await db.SaveChangesAsync();
        }

        await _provider.DisposeAsync();
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private async Task<T> WithStore<T>(Func<ModuleHoldStore, Task<T>> action)
    {
        using var scope = _provider.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ModuleHoldStore>());
    }

    private Task<DateTime> Hold(Guid? moduleId = null) =>
        WithStore(async s =>
        {
            await s.Hold(ModuleHoldKey.Module(_organizationId, moduleId ?? _moduleId), 20);
            return (await s.GetHoldState(_organizationId, moduleId ?? _moduleId)).ExpiresAt!.Value;
        });

    private async Task SetSaga(DesiredStateHeadline? queued = null, QueuedReason? reason = null)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
        if (saga == null)
        {
            saga = new ModuleSaga { CorrelationId = _moduleId, OrganizationId = _organizationId, CurrentState = "Gatekeeping" };
            db.ModuleSagas.Add(saga);
        }

        saga.CurrentState = "Gatekeeping";
        saga.Paused = false;
        saga.QueuedDesiredStateHeadline = queued;
        saga.QueuedReason = reason;
        await db.SaveChangesAsync();
    }

    private async Task<ModuleSaga> WaitForSaga(Func<ModuleSaga, bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        ModuleSaga saga;
        do
        {
            await using var db = _fixture.CreateDbContext();
            saga = await db.ModuleSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == _moduleId);
            if (predicate(saga)) return saga;
            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);

        return saga;
    }

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }

        return condition();
    }

    private Task<bool> Consumed<T>(Func<IReceivedMessage<T>, bool> predicate) where T : class =>
        WaitUntil(() => _harness.Consumed.Select<T>().Any(predicate));

    private int ConsumedCount<T>() where T : class => _harness.Consumed.Select<T>().Count();

    private async Task ConsumedOneMore<T>(int before) where T : class
    {
        Assert.True(await WaitUntil(() => ConsumedCount<T>() > before));
        await Task.Delay(300);
    }

    private async Task<Guid> SeedFinishedJob()
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            TimestampStart = DateTimeOffset.UtcNow,
            TimestampEnd = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Completed,
            JobType = "Apply",
            IsCurrent = null
        });
        await db.SaveChangesAsync();
        _seededJobs.Add(jobId);
        return jobId;
    }

    private async Task Trigger(DesiredStateHeadline state = DesiredStateHeadline.Applied, bool setNew = true)
    {
        var before = ConsumedCount<GatekeepingJobRequested>();
        await _harness.Bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            DesiredStateHeadline = state,
            SetNewDesiredState = setNew
        });
        await ConsumedOneMore<GatekeepingJobRequested>(before);
    }

    private async Task Check()
    {
        var before = ConsumedCount<ModuleHoldCheck>();
        await _harness.Bus.Publish(new ModuleHoldCheck { ModuleId = _moduleId, OrganizationId = _organizationId });
        await ConsumedOneMore<ModuleHoldCheck>(before);
    }

    private List<(DateTime Time, ModuleHoldCheck Message)> ScheduledChecks() =>
        _scheduled.Where(c => c.Message.ModuleId == _moduleId).ToList();

    [Fact]
    public async Task A_Trigger_For_A_Held_Module_Is_Parked_And_A_Check_Scheduled_Just_After_Expiry()
    {
        await SetSaga();
        var expiresAt = await Hold();

        await Trigger();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.HeldBySession);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
        var check = Assert.Single(ScheduledChecks());
        Assert.Equal(expiresAt.AddSeconds(1), check.Time);
    }

    [Fact]
    public async Task A_Trigger_For_A_Module_In_A_Held_Namespace_Is_Parked_And_Released_When_It_Lapses()
    {
        await SetSaga();
        await WithStore(async s => { await s.Hold(ModuleHoldKey.Namespace(_organizationId, _fixture.Namespaces["000"].Id), 20); return true; });

        await Trigger();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.HeldBySession);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
        Assert.Empty(_applied);
        Assert.Single(ScheduledChecks());

        _clock.Advance(TimeSpan.FromSeconds(21));
        await Check();

        saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedReason);
        Assert.Contains(_moduleId, _applied);
    }

    [Fact]
    public async Task A_Trigger_For_A_Module_In_A_Held_Stack_Is_Parked()
    {
        await SetSaga();
        await WithStore(async s => { await s.Hold(ModuleHoldKey.Stack(_organizationId, _fixture.Stacks["00"].Id), 20); return true; });

        await Trigger();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.HeldBySession);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Further_Triggers_While_Held_Schedule_No_Further_Check()
    {
        await SetSaga();
        await Hold();

        await Trigger();
        await Trigger();
        await Trigger(setNew: false);

        Assert.Single(ScheduledChecks());
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task An_Explicit_Request_While_Held_Replaces_What_Is_Queued()
    {
        await SetSaga();
        await Hold();

        await Trigger();
        await Trigger(DesiredStateHeadline.Destroyed);

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == DesiredStateHeadline.Destroyed);
        Assert.Equal(DesiredStateHeadline.Destroyed, saga.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
    }

    [Fact]
    public async Task Work_Queued_Before_The_Hold_Is_Not_Released_By_A_Finished_Job()
    {
        await SetSaga(DesiredStateHeadline.Applied, QueuedReason.WaitingOnRunningJob);
        await Hold();
        var jobId = await SeedFinishedJob();

        var before = ConsumedCount<ApplyModuleCompleted>();
        await _harness.Bus.Publish(new ApplyModuleCompleted { ModuleId = _moduleId, OrganizationId = _organizationId, ModuleJobId = jobId });
        await ConsumedOneMore<ApplyModuleCompleted>(before);

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.HeldBySession);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
        Assert.Single(ScheduledChecks());
    }

    [Fact]
    public async Task A_Check_While_The_Hold_Lasts_Reschedules_Just_After_The_New_Expiry()
    {
        await SetSaga();
        await Hold();
        await Trigger();

        _clock.Advance(TimeSpan.FromSeconds(15));
        var refreshed = await Hold();
        await Check();

        var checks = ScheduledChecks();
        Assert.Equal(2, checks.Count);
        Assert.Equal(refreshed.AddSeconds(1), checks[1].Time);
        var saga = await WaitForSaga(_ => true);
        Assert.Equal(QueuedReason.HeldBySession, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task A_Check_After_The_Hold_Expires_Releases_The_Queue_Once()
    {
        await SetSaga();
        await Hold();
        await Trigger();

        _clock.Advance(TimeSpan.FromSeconds(21));
        await Check();

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedDesiredStateHeadline);
        Assert.Null(saga.QueuedReason);
        Assert.Single(_applied);

        await Check();
        Assert.Single(_applied);
        Assert.Single(ScheduledChecks());
    }

    [Fact]
    public async Task A_Missing_Hold_Entry_Releases_The_Queue()
    {
        await SetSaga(DesiredStateHeadline.Applied, QueuedReason.HeldBySession);

        await Check();

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedReason);
        Assert.Contains(_moduleId, _applied);
    }

    [Fact]
    public async Task A_Check_For_A_Module_Not_Parked_On_A_Hold_Does_Nothing()
    {
        await SetSaga(DesiredStateHeadline.Applied, QueuedReason.WaitingOnRunnerCheckin);

        await Check();

        var saga = await WaitForSaga(_ => true);
        Assert.Equal(QueuedReason.WaitingOnRunnerCheckin, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
        Assert.Empty(ScheduledChecks());
    }

    [Fact]
    public async Task A_Module_That_Is_Not_Held_Runs_As_Before()
    {
        await SetSaga();
        await Hold(_fixture.Modules["0001"].Id);

        await Trigger();

        Assert.True(await WaitUntil(() => _applied.Contains(_moduleId)));
        var saga = await WaitForSaga(_ => true);
        Assert.Null(saga.QueuedReason);
        Assert.Empty(ScheduledChecks());
    }

    [Fact]
    public async Task Reconciliation_Re_Arms_A_Parked_Module_Whose_Check_Was_Lost()
    {
        await SetSaga();
        await Hold();
        await Trigger();
        _clock.Advance(TimeSpan.FromSeconds(21));

        var before = ConsumedCount<ModuleHoldCheck>();
        var job = new TransportReconciliationJob(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext(),
            _provider.GetRequiredService<IBus>(),
            Mock.Of<IMessageScheduler>(),
            new QuotaService(Mock.Of<IQuotaGatingService>()),
            NullLogger<TransportReconciliationJob>.Instance);
        await job.ExecuteJob();
        await ConsumedOneMore<ModuleHoldCheck>(before);

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedReason);
        Assert.Contains(_moduleId, _applied);
    }

    private sealed class StubJobService(SnapCdDbContext dbContext, ConcurrentBag<Guid> applied)
        : JobService(null!, dbContext, null!, null!, null!, null!, null!, null!, null!)
    {
        public override Task Apply(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null, string? sourceRevisionOverride = null)
        {
            applied.Add(moduleId);
            return Task.CompletedTask;
        }

        public override Task Destroy(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null, string? sourceRevisionOverride = null)
            => Task.CompletedTask;

        public override Task<bool> CheckDependenciesAsync(Guid moduleId, Guid organizationId, DesiredStateHeadline desiredState) => Task.FromResult(true);

        public override Task<bool> CheckRunnerAvailabilityAsync(Guid moduleId) => Task.FromResult(true);
    }
}
