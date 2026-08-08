// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>
/// A mission occupies an agent and writes its result back over the hub, so an in-flight one holds
/// Draining open exactly as an in-flight job task does.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MaintenancePhaseCriteriaTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private Module _module = null!;
    private readonly List<Guid> _jobs = [];

    public MaintenancePhaseCriteriaTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        using var db = _fixture.CreateDbContext();
        _module = db.Modules.First(m => m.Id == _fixture.Modules["0000"].Id);

        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddMassTransitTestHarness();
        _provider = services.BuildServiceProvider(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            db.ModuleJobMissionRuns.RemoveRange(db.ModuleJobMissionRuns.Where(r => _jobs.Contains(r.ModuleJobId)));
            db.ModuleJobMissions.RemoveRange(db.ModuleJobMissions.Where(m => _jobs.Contains(m.ModuleJobId)));
            db.ModuleJobs.RemoveRange(db.ModuleJobs.Where(j => _jobs.Contains(j.Id)));
            await db.SaveChangesAsync();
        }

        await _provider.DisposeAsync();
    }

    private MaintenancePhaseService CreateService() =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            new TransportProbeService(
                Options.Create(new ServiceBusSettings()),
                new ConfigurationBuilder().Build()));

    /// <summary>Seeds the job -> mission -> run chain a run row requires.</summary>
    private async Task SeedRun(MissionStatus status, string? connectionId = null)
    {
        var jobId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        _jobs.Add(jobId);

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
        db.ModuleJobMissions.Add(new ModuleJobMission
        {
            Id = missionId,
            OrganizationId = _module.OrganizationId,
            ModuleJobId = jobId,
            MissionId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            MissionType = MissionType.SummarizeJob
        });
        db.ModuleJobMissionRuns.Add(new ModuleJobMissionRun
        {
            Id = Guid.NewGuid(),
            OrganizationId = _module.OrganizationId,
            ModuleJobMissionId = missionId,
            ModuleJobId = jobId,
            MissionType = MissionType.SummarizeJob,
            AgentId = Guid.NewGuid(),
            InvocationId = Guid.NewGuid(),
            AttemptNumber = 1,
            Status = status,
            ServerInstanceId = connectionId is null ? null : Guid.NewGuid(),
            SignalRConnectionId = connectionId,
            DeadlineAt = DateTime.UtcNow.AddMinutes(2)
        });
        await db.SaveChangesAsync();
    }

    private MaintenanceOperationsService CreateOperations() =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(),
            _provider.GetRequiredService<IBus>(),
            null!,
            // CancelHoldingMissionsAsync touches neither the reconciler nor the mode service.
            null!,
            NullLogger<MaintenanceOperationsService>.Instance);

    private async Task<PhaseCriterion> MissionCriterion()
    {
        var readiness = await CreateService().EvaluateAsync(MaintenancePhase.Draining);
        return readiness.Criteria.Single(c => c.Name == "No mission mid-run");
    }

    [Fact]
    public async Task Cancelling_holding_missions_marks_runs_with_no_live_agent_and_leaves_parked_ones()
    {
        await SeedRun(MissionStatus.Running);              // no connection seeded -> marked
        await SeedRun(MissionStatus.AwaitingReconnect);    // no connection seeded -> marked
        await SeedRun(MissionStatus.WaitingForAgent);      // parked, not holding the drain

        var result = await CreateOperations().CancelHoldingMissionsAsync();

        Assert.Equal(2, result.MarkedCancelled);
        Assert.Empty(result.Skipped);
        Assert.True((await MissionCriterion()).IsMet);

        await using var db = _fixture.CreateDbContext();
        var parked = await db.ModuleJobMissionRuns.AsNoTracking()
            .Where(r => _jobs.Contains(r.ModuleJobId) && r.Status == MissionStatus.WaitingForAgent)
            .CountAsync();
        // A parked run survives a window untouched, so cancelling must not reach it.
        Assert.Equal(1, parked);
    }

    [Fact]
    public async Task A_run_holding_a_dead_connection_id_is_marked_rather_than_asked()
    {
        // AwaitingReconnect keeps the id it was dispatched on, so the id alone does not mean the
        // agent is there: asking it sends into a void and the run never stops.
        await SeedRun(MissionStatus.AwaitingReconnect, "stale-connection-id");

        var result = await CreateOperations().CancelHoldingMissionsAsync();

        Assert.Equal(0, result.Requested);
        Assert.Equal(1, result.MarkedCancelled);
        Assert.True((await MissionCriterion()).IsMet);
    }

    [Fact]
    public async Task Draining_waits_on_the_transport_as_well_as_the_database()
    {
        var readiness = await CreateService().EvaluateAsync(MaintenancePhase.Draining);

        // Draining absorbed the transport check: leaving it means drained everywhere, not just
        // that the database has gone quiet while messages are still in flight.
        Assert.Contains(readiness.Criteria, c => c.Name == "Transport idle");
        Assert.Contains(readiness.Criteria, c => c.Name == "No job mid-step");
    }

    [Fact]
    public async Task Draining_is_blocked_by_a_running_mission()
    {
        await SeedRun(MissionStatus.Running);

        var criterion = await MissionCriterion();

        Assert.False(criterion.IsMet);
        Assert.Equal(1, criterion.Count);
    }

    [Fact]
    public async Task Draining_is_blocked_by_a_mission_awaiting_reconnect()
    {
        await SeedRun(MissionStatus.AwaitingReconnect);

        Assert.False((await MissionCriterion()).IsMet);
    }

    [Fact]
    public async Task Draining_is_not_blocked_by_a_parked_or_finished_mission()
    {
        await SeedRun(MissionStatus.WaitingForAgent);
        await SeedRun(MissionStatus.BlockedAgentNotAssigned);
        await SeedRun(MissionStatus.Succeeded);

        Assert.True((await MissionCriterion()).IsMet);
    }
}
