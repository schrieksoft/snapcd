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
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// The pause gate on ModuleSaga: a paused Module parks triggered work, refuses to dequeue it,
/// re-drives it on resume, and says when it has gone quiet. The job graph is stood in for by a
/// stub JobService that records what the gatekeeper asked it to run.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class PauseGateTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private IMaintenanceModeService _maintenance = null!;
    private readonly ConcurrentBag<Guid> _applied = [];
    private readonly List<Guid> _seededJobs = [];
    private Guid _moduleId;
    private Guid _organizationId;

    public PauseGateTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
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

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();

        using var scope = _provider.CreateScope();
        _maintenance = scope.ServiceProvider.GetRequiredService<IMaintenanceModeService>();
        await _maintenance.DisableAsync();
    }

    public async Task DisposeAsync()
    {
        using (var scope = _provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IMaintenanceModeService>().DisableAsync();

        await using (var db = _fixture.CreateDbContext())
        {
            var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
            if (saga != null)
            {
                saga.Paused = false;
                saga.PausedBy = null;
                saga.PausedAt = null;
                saga.PauseReason = null;
                saga.QueuedDesiredStateHeadline = null;
                saga.QueuedReason = null;
                saga.HeldByTransferId = null;
                saga.HeldAt = null;
            }

            db.ModuleJobs.RemoveRange(db.ModuleJobs.Where(j => _seededJobs.Contains(j.Id)));
            await db.SaveChangesAsync();
        }

        await _provider.DisposeAsync();
    }

    private async Task SetSaga(bool paused, DesiredStateHeadline? queued = null, QueuedReason? reason = null,
        Guid? heldBy = null)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
        if (saga == null)
        {
            saga = new ModuleSaga { CorrelationId = _moduleId, OrganizationId = _organizationId, CurrentState = "Gatekeeping" };
            db.ModuleSagas.Add(saga);
        }

        saga.CurrentState = "Gatekeeping";
        saga.Paused = paused;
        saga.PausedBy = paused ? Guid.NewGuid() : null;
        saga.PausedAt = paused ? DateTime.UtcNow : null;
        saga.QueuedDesiredStateHeadline = queued;
        saga.QueuedReason = reason;
        saga.HeldByTransferId = heldBy;
        saga.HeldAt = heldBy != null ? DateTime.UtcNow : null;
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

    private async Task<Guid> SeedRunningJob()
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
            IsCurrent = true
        });
        await db.SaveChangesAsync();
        _seededJobs.Add(jobId);
        return jobId;
    }

    private async Task FinishJob(Guid jobId)
    {
        await using var db = _fixture.CreateDbContext();
        var job = await db.ModuleJobs.SingleAsync(j => j.Id == jobId);
        job.IsCurrent = null;
        job.Status = ExecutionStatus.Completed;
        job.TimestampEnd = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    // The harness's Any() stops waiting once its inactivity timer has fired, which a slow fixture
    // setup can trip before the first publish; a deadline poll does not have that edge.
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

    private Task<bool> QuietWasPublished() => WaitUntil(QuietPublishedSoFar);

    private Task<bool> Consumed<T>(Func<IReceivedMessage<T>, bool> predicate) where T : class =>
        WaitUntil(() => _harness.Consumed.Select<T>().Any(predicate));

    private bool QuietPublishedSoFar() =>
        _harness.Published.Select<ModuleQuiet>(x => x.Context.Message.ModuleId == _moduleId).Any();

    private Task PublishTrigger() => _harness.Bus.Publish(new GatekeepingJobRequested
    {
        ModuleId = _moduleId,
        OrganizationId = _organizationId,
        DesiredStateHeadline = DesiredStateHeadline.Applied,
        SetNewDesiredState = true
    });

    private Task PublishDependencyCheck() => _harness.Bus.Publish(new ModuleDependencyCheckRequested
    {
        ModuleId = _moduleId,
        OrganizationId = _organizationId
    });

    /// <summary>A hold closes the same gate a pause does, and says so with its own queued reason.</summary>
    [Fact]
    public async Task Trigger_On_A_Held_Module_Parks_And_Starts_Nothing()
    {
        await SetSaga(paused: false, heldBy: Guid.NewGuid());

        await PublishTrigger();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Held);
        Assert.Equal(QueuedReason.Held, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Run_Queue_Now_On_A_Held_Module_Leaves_The_Queue_Untouched()
    {
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held, heldBy: Guid.NewGuid());

        await _harness.Bus.Publish(new RunQueueNowRequested { ModuleId = _moduleId, OrganizationId = _organizationId });
        Assert.True(await Consumed<RunQueueNowRequested>(x => x.Context.Message.ModuleId == _moduleId));

        await Task.Delay(500);
        var saga = await WaitForSaga(_ => true);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.Held, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Dependency_Check_On_A_Held_Module_Leaves_The_Queue_Untouched()
    {
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held, heldBy: Guid.NewGuid());

        await PublishDependencyCheck();
        Assert.True(await Consumed<ModuleDependencyCheckRequested>(x => x.Context.Message.ModuleId == _moduleId));

        await Task.Delay(500);
        var saga = await WaitForSaga(_ => true);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.Held, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Release_Re_Drives_Parked_Work()
    {
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held, heldBy: Guid.NewGuid());

        // ReleaseHold does exactly these two things: clear the columns, then publish a dependency
        // check so the gatekeeper re-evaluates rather than forcing execution.
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held);
        await PublishDependencyCheck();

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedDesiredStateHeadline);
        Assert.Null(saga.QueuedReason);
        Assert.Contains(_moduleId, _applied);
    }

    /// <summary>Releasing a hold leaves an operator's pause standing: the two flags are independent.</summary>
    [Fact]
    public async Task Releasing_A_Hold_Does_Not_Resume_A_Paused_Module()
    {
        await SetSaga(paused: true, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held, heldBy: Guid.NewGuid());

        await SetSaga(paused: true, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held);
        await PublishDependencyCheck();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Paused);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
    }

    /// <summary>
    /// A withdrawal must not open the gate: after a merge the Module's code and state disagree, so
    /// the hold becomes a pause rather than a release.
    /// </summary>
    [Fact]
    public async Task A_Withdrawal_Turns_The_Hold_Into_A_Pause()
    {
        var transferId = Guid.NewGuid();
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Held, heldBy: transferId);

        using var repository = new ModuleSagaRepository(_fixture.CreateDbContext(), _fixture.CreateMockBus());
        var converted = await repository.ConvertHoldToPause(
            _moduleId, _organizationId, transferId, "withdrawn from transfer; state not migrated");

        Assert.True(converted);

        await PublishDependencyCheck();
        await Task.Delay(500);

        var saga = await WaitForSaga(_ => true);
        Assert.Null(saga.HeldByTransferId);
        Assert.True(saga.Paused);
        Assert.Equal("withdrawn from transfer; state not migrated", saga.PauseReason);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
    }

    /// <summary>A release naming a different transfer leaves the hold alone.</summary>
    [Fact]
    public async Task A_Stale_Withdrawal_Does_Not_Free_The_Module()
    {
        var holder = Guid.NewGuid();
        await SetSaga(paused: false, heldBy: holder);

        using var repository = new ModuleSagaRepository(_fixture.CreateDbContext(), _fixture.CreateMockBus());
        var converted = await repository.ConvertHoldToPause(_moduleId, _organizationId, Guid.NewGuid(), "stale");

        Assert.False(converted);

        var saga = await WaitForSaga(_ => true);
        Assert.Equal(holder, saga.HeldByTransferId);
        Assert.False(saga.Paused);
    }

    [Fact]
    public async Task Trigger_On_A_Paused_Module_Parks_And_Starts_Nothing()
    {
        await SetSaga(paused: true);

        await PublishTrigger();

        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Paused);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Run_Queue_Now_On_A_Paused_Module_Leaves_The_Queue_Untouched()
    {
        await SetSaga(paused: true, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Paused);

        await _harness.Bus.Publish(new RunQueueNowRequested { ModuleId = _moduleId, OrganizationId = _organizationId });
        Assert.True(await Consumed<RunQueueNowRequested>(x => x.Context.Message.ModuleId == _moduleId));

        await Task.Delay(500);
        var saga = await WaitForSaga(_ => true);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Dependency_Check_On_A_Paused_Module_Leaves_The_Queue_Untouched()
    {
        await SetSaga(paused: true, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Paused);

        await PublishDependencyCheck();
        Assert.True(await Consumed<ModuleDependencyCheckRequested>(x => x.Context.Message.ModuleId == _moduleId));

        await Task.Delay(500);
        var saga = await WaitForSaga(_ => true);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task Resume_Re_Drives_Parked_Work()
    {
        await SetSaga(paused: true, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Paused);

        // The repository's SetPaused(false) does exactly these two things: clear the flag, then
        // publish a dependency check so the gatekeeper re-evaluates rather than forcing execution.
        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Paused);
        await PublishDependencyCheck();

        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedDesiredStateHeadline);
        Assert.Null(saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.DesiredStateHeadline);
        Assert.Contains(_moduleId, _applied);
    }

    [Fact]
    public async Task Quiescence_Request_On_A_Quiet_Paused_Module_Is_Answered_At_Once()
    {
        await SetSaga(paused: true);

        await _harness.Bus.Publish(new ModuleQuiescenceRequested { ModuleId = _moduleId, OrganizationId = _organizationId });

        Assert.True(await QuietWasPublished());
    }

    [Fact]
    public async Task Quiescence_Request_While_A_Job_Runs_Is_Answered_When_It_Completes()
    {
        await SetSaga(paused: true);
        var jobId = await SeedRunningJob();

        await _harness.Bus.Publish(new ModuleQuiescenceRequested { ModuleId = _moduleId, OrganizationId = _organizationId });
        Assert.True(await Consumed<ModuleQuiescenceRequested>(x => x.Context.Message.ModuleId == _moduleId));
        await Task.Delay(500);
        Assert.False(QuietPublishedSoFar());

        // A trigger arriving while the module is still draining parks; it does not wait for quiet.
        await PublishTrigger();
        var saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Paused);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);

        await FinishJob(jobId);
        await _harness.Bus.Publish(new ApplyModuleCompleted { ModuleId = _moduleId, OrganizationId = _organizationId, ModuleJobId = jobId });

        Assert.True(await QuietWasPublished());
        saga = await WaitForSaga(_ => true);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Empty(_applied);
    }

    [Fact]
    public async Task A_Failed_Job_Also_Answers_Quiet()
    {
        await SetSaga(paused: true);
        var jobId = await SeedRunningJob();
        await FinishJob(jobId);

        await _harness.Bus.Publish(new ApplyModuleFailed { ModuleId = _moduleId, OrganizationId = _organizationId, ModuleJobId = jobId });

        Assert.True(await QuietWasPublished());
    }

    [Fact]
    public async Task A_Cancelled_Job_Also_Answers_Quiet()
    {
        await SetSaga(paused: true);
        var jobId = await SeedRunningJob();
        await FinishJob(jobId);

        await _harness.Bus.Publish(new ApplyModuleCancelled { ModuleId = _moduleId, OrganizationId = _organizationId, ModuleJobId = jobId });

        Assert.True(await QuietWasPublished());
    }

    [Fact]
    public async Task An_Unpaused_Module_Does_Not_Announce_Quiet_On_Completion()
    {
        await SetSaga(paused: false);
        var jobId = await SeedRunningJob();
        await FinishJob(jobId);

        await _harness.Bus.Publish(new ApplyModuleCompleted { ModuleId = _moduleId, OrganizationId = _organizationId, ModuleJobId = jobId });
        Assert.True(await Consumed<ApplyModuleCompleted>(x => x.Context.Message.ModuleJobId == jobId));

        await Task.Delay(500);
        Assert.False(QuietPublishedSoFar());
    }

    [Fact]
    public async Task Maintenance_And_Pause_Together_Park_Once_And_Lift_Independently()
    {
        await SetSaga(paused: true);
        using var scope = _provider.CreateScope();
        var maintenance = scope.ServiceProvider.GetRequiredService<IMaintenanceModeService>();
        await maintenance.EnableAsync(Guid.NewGuid(), "pause gate test");

        await PublishTrigger();
        var saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == DesiredStateHeadline.Applied);
        Assert.Equal(QueuedReason.Maintenance, saga.QueuedReason);

        await maintenance.DisableAsync();
        await PublishDependencyCheck();
        saga = await WaitForSaga(s => s.QueuedReason == QueuedReason.Paused);
        Assert.Equal(QueuedReason.Paused, saga.QueuedReason);
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Empty(_applied);

        await SetSaga(paused: false, queued: DesiredStateHeadline.Applied, reason: QueuedReason.Paused);
        await PublishDependencyCheck();
        saga = await WaitForSaga(s => s.QueuedDesiredStateHeadline == null);
        Assert.Null(saga.QueuedReason);
        Assert.Contains(_moduleId, _applied);
    }

    private sealed class StubJobService(SnapCdDbContext dbContext, ConcurrentBag<Guid> applied)
        : JobService(null!, dbContext, null!, null!, null!, null!, null!, null!, null!, null!)
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
