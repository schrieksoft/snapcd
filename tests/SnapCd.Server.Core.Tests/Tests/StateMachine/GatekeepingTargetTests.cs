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
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.ModuleHold;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// The gate acts on the module's target (queued state, else desired state) for a request that does not set a new
/// state, checks dependencies for the state it will actually run, and never queues a destroy on such a request.
/// The job graph is a stub that records what it was asked to run and answers dependency checks per direction.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class GatekeepingTargetTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private readonly ConcurrentQueue<(Guid ModuleId, DesiredStateHeadline State)> _ran = new();
    private readonly List<Guid> _seededJobs = [];
    private readonly Dependencies _dependencies = new();
    private Guid _moduleId;
    private Guid _organizationId;

    public GatekeepingTargetTests(Fixture fixture) => _fixture = fixture;

    private sealed class Dependencies
    {
        public bool ApplyMet { get; set; } = true;
        public bool DestroyMet { get; set; } = true;
    }

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddSingleton(TimeProvider.System);
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
        services.AddScoped<JobService>(sp => new StubJobService(sp.GetRequiredService<SnapCdDbContext>(), _ran, _dependencies));
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
        await scope.ServiceProvider.GetRequiredService<IMaintenanceModeService>().DisableAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
            if (saga != null)
            {
                saga.Paused = false;
                saga.PausedBy = null;
                saga.PausedAt = null;
                saga.DesiredStateHeadline = DesiredStateHeadline.Applied;
                saga.QueuedDesiredStateHeadline = null;
                saga.QueuedReason = null;
            }

            db.ModuleJobs.RemoveRange(db.ModuleJobs.Where(j => _seededJobs.Contains(j.Id)));
            await db.SaveChangesAsync();
        }

        await _provider.DisposeAsync();
    }

    private async Task SetSaga(DesiredStateHeadline? desired, DesiredStateHeadline? queued = null, bool paused = false)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.ModuleSagas.SingleOrDefaultAsync(s => s.CorrelationId == _moduleId);
        if (saga == null)
        {
            saga = new ModuleSaga { CorrelationId = _moduleId, OrganizationId = _organizationId, CurrentState = "Gatekeeping" };
            db.ModuleSagas.Add(saga);
        }

        saga.CurrentState = "Gatekeeping";
        saga.DesiredStateHeadline = desired;
        saga.QueuedDesiredStateHeadline = queued;
        saga.QueuedReason = queued == null ? null : QueuedReason.WaitingOnDependencies;
        saga.Paused = paused;
        saga.PausedBy = paused ? Guid.NewGuid() : null;
        saga.PausedAt = paused ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
    }

    private async Task SeedJob(string jobType, bool running, ActualStateHeadline? leftItIn = null)
    {
        var jobId = Guid.NewGuid();
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-5),
            TimestampEnd = running ? null : DateTimeOffset.UtcNow,
            Status = running ? ExecutionStatus.Running : ExecutionStatus.Completed,
            JobType = jobType,
            IsCurrent = running ? true : null,
            ActualStateHeadline = leftItIn
        });
        await db.SaveChangesAsync();
        _seededJobs.Add(jobId);
    }

    private async Task<ModuleSaga> ReadSaga()
    {
        await using var db = _fixture.CreateDbContext();
        return await db.ModuleSagas.AsNoTracking().SingleAsync(s => s.CorrelationId == _moduleId);
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

    /// <summary>Sends a trigger and waits until the saga has handled it.</summary>
    private async Task Trigger(DesiredStateHeadline? state = null, bool setNew = false, Guid? probeWaveId = null)
    {
        var jobId = Guid.NewGuid();
        await _harness.Bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            DesiredStateHeadline = state,
            SetNewDesiredState = setNew,
            ProbeWaveId = probeWaveId,
            JobId = jobId
        });
        Assert.True(await WaitUntil(() => _harness.Consumed.Select<GatekeepingJobRequested>(m => m.Context.Message.JobId == jobId).Any()));
    }

    private bool Ran(DesiredStateHeadline state) => _ran.Any(r => r.ModuleId == _moduleId && r.State == state);

    /// <summary>
    /// A wave asks an extra question, never a replacement one. A Module whose dependencies are not
    /// satisfied queues like any other, which is what keeps a Module that has never applied out of
    /// an apply it cannot run: its upstream has no state and no outputs for it to read.
    /// </summary>
    [Fact]
    public async Task A_probe_wave_does_not_release_a_module_whose_dependencies_are_unmet()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: null);
        _dependencies.ApplyMet = false;

        await Trigger(probeWaveId: Guid.NewGuid());

        Assert.False(Ran(DesiredStateHeadline.Applied));
        Assert.Equal(DesiredStateHeadline.Applied, (await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_checks_a_queued_destroy_against_its_dependents()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: DesiredStateHeadline.Destroyed);
        _dependencies.ApplyMet = true;
        _dependencies.DestroyMet = false;

        await Trigger();

        Assert.False(Ran(DesiredStateHeadline.Destroyed));
        Assert.Equal(DesiredStateHeadline.Destroyed, (await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_releases_a_queued_destroy_once_its_dependents_are_gone()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: DesiredStateHeadline.Destroyed);
        _dependencies.DestroyMet = true;

        await Trigger();

        Assert.True(Ran(DesiredStateHeadline.Destroyed));
        var saga = await ReadSaga();
        Assert.Null(saga.QueuedDesiredStateHeadline);
        Assert.Equal(DesiredStateHeadline.Destroyed, saga.DesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_during_a_running_destroy_queues_nothing()
    {
        await SetSaga(desired: DesiredStateHeadline.Destroyed);
        await SeedJob("Destroy", running: true);

        await Trigger();

        Assert.Null((await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_while_paused_does_not_queue_a_destroy()
    {
        await SetSaga(desired: DesiredStateHeadline.Destroyed, paused: true);

        await Trigger();

        Assert.Null((await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_during_an_apply_keeps_a_queued_destroy()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: DesiredStateHeadline.Destroyed);
        await SeedJob("Apply", running: true);

        await Trigger();

        Assert.Equal(DesiredStateHeadline.Destroyed, (await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task An_explicit_apply_replaces_a_queued_destroy()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: DesiredStateHeadline.Destroyed);
        await SeedJob("Apply", running: true);

        await Trigger(DesiredStateHeadline.Applied, setNew: true);

        var saga = await ReadSaga();
        Assert.Equal(DesiredStateHeadline.Applied, saga.QueuedDesiredStateHeadline);
        Assert.Equal(DesiredStateHeadline.Applied, saga.DesiredStateHeadline);
    }

    [Fact]
    public async Task An_automatic_trigger_still_applies_an_applied_module()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied);
        _dependencies.ApplyMet = true;

        await Trigger();

        Assert.True(Ran(DesiredStateHeadline.Applied));
    }

    [Fact]
    public async Task An_automatic_trigger_never_starts_a_destroy()
    {
        // Desired is Destroyed but the last destroy did not finish the job: only an explicit request retries it.
        await SetSaga(desired: DesiredStateHeadline.Destroyed);

        await Trigger();

        Assert.False(Ran(DesiredStateHeadline.Destroyed));
        Assert.Null((await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task A_named_state_that_is_not_the_target_does_nothing()
    {
        await SetSaga(desired: DesiredStateHeadline.Applied, queued: DesiredStateHeadline.Destroyed);
        _dependencies.ApplyMet = true;
        _dependencies.DestroyMet = true;

        await Trigger(DesiredStateHeadline.Applied, setNew: false);

        Assert.Empty(_ran);
        Assert.Equal(DesiredStateHeadline.Destroyed, (await ReadSaga()).QueuedDesiredStateHeadline);
    }

    [Fact]
    public async Task A_queued_destroy_of_a_destroyed_module_is_dropped_on_dequeue()
    {
        await SetSaga(desired: DesiredStateHeadline.Destroyed, queued: DesiredStateHeadline.Destroyed);
        await SeedJob("Destroy", running: false, leftItIn: ActualStateHeadline.Destroyed);
        _dependencies.DestroyMet = true;

        await _harness.Bus.Publish(new ModuleDependencyCheckRequested { ModuleId = _moduleId, OrganizationId = _organizationId });
        Assert.True(await WaitUntil(() => _harness.Consumed.Select<ModuleDependencyCheckRequested>(m => m.Context.Message.ModuleId == _moduleId).Any()));

        Assert.False(Ran(DesiredStateHeadline.Destroyed));
        Assert.Null((await ReadSaga()).QueuedDesiredStateHeadline);
    }

    private sealed class StubJobService(SnapCdDbContext dbContext, ConcurrentQueue<(Guid, DesiredStateHeadline)> ran, Dependencies dependencies)
        : JobService(null!, dbContext, null!, null!, null!, null!, null!, null!, null!, null!)
    {
        public override Task Apply(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null, string? sourceRevisionOverride = null)
        {
            ran.Enqueue((moduleId, DesiredStateHeadline.Applied));
            return Task.CompletedTask;
        }

        public override Task Destroy(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null, string? sourceRevisionOverride = null)
        {
            ran.Enqueue((moduleId, DesiredStateHeadline.Destroyed));
            return Task.CompletedTask;
        }

        public override Task<bool> CheckDependenciesAsync(Guid moduleId, Guid organizationId, DesiredStateHeadline desiredState) =>
            Task.FromResult(desiredState == DesiredStateHeadline.Applied ? dependencies.ApplyMet : dependencies.DestroyMet);

        public override Task<bool> CheckRunnerAvailabilityAsync(Guid moduleId) => Task.FromResult(true);
    }
}
