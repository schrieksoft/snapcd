// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Services.Ai.Missions;

namespace SnapCd.Server.Core.Consumers.Missions;

/// <summary>
/// The run watchdog. Fired by the scheduled <see cref="MissionRunDeadlineCheck"/> at a run's
/// <c>DeadlineAt</c>: if a heartbeat moved the deadline it re-arms; if the deadline lapsed (the run
/// went silent — orchestrator died, instance died uncleanly, or the reconnect grace expired) it marks
/// the run <c>TimedOut</c> and either retries (next attempt, via <see cref="MissionDispatcher"/>) or
/// fails the mission. Competing — exactly one instance handles each tick.
/// </summary>
public class MissionRunDeadlineCheckConsumer : IConsumer<MissionRunDeadlineCheck>
{
    public const int MaxAttempts = 3;

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly MissionDispatcher _dispatcher;
    private readonly IMessageScheduler _scheduler;
    private readonly ILogger<MissionRunDeadlineCheckConsumer> _logger;

    public MissionRunDeadlineCheckConsumer(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        MissionDispatcher dispatcher,
        IMessageScheduler scheduler,
        ILogger<MissionRunDeadlineCheckConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MissionRunDeadlineCheck> context)
    {
        var ct = context.CancellationToken;
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var run = await db.ModuleJobMissionRuns
            .FirstOrDefaultAsync(r => r.Id == context.Message.RunId && r.OrganizationId == context.Message.OrganizationId, ct);
        if (run is null)
            return;

        // Terminal already — nothing to watch.
        if (run.Status is MissionStatus.Succeeded or MissionStatus.Failed
            or MissionStatus.Cancelled or MissionStatus.TimedOut)
            return;

        // A heartbeat (or reconnect) pushed the deadline out — re-arm one check for the new time.
        if (DateTime.UtcNow < run.DeadlineAt)
        {
            await _scheduler.SchedulePublish(run.DeadlineAt, context.Message, ct);
            return;
        }

        // Deadline lapsed: the run is silent. Recover it.
        var wasAwaitingReconnect = run.Status == MissionStatus.AwaitingReconnect;
        run.Status = MissionStatus.TimedOut;
        run.CompletedAt = DateTime.UtcNow;
        run.Error = wasAwaitingReconnect
            ? "Agent did not reconnect within the grace window."
            : "No heartbeat within the timeout window.";
        await db.SaveChangesAsync(ct); // frees the filtered-unique lock before any re-claim

        var mission = await db.ModuleJobMissions
            .FirstOrDefaultAsync(m => m.Id == run.ModuleJobMissionId && m.OrganizationId == run.OrganizationId, ct);
        if (mission is null)
            return;

        if (run.AttemptNumber < MaxAttempts)
        {
            _logger.LogWarning("Run {RunId} timed out; retrying mission {MissionType} (attempt {Next}/{Max}).",
                run.Id, run.MissionType, run.AttemptNumber + 1, MaxAttempts);
            var outcome = await _dispatcher.DispatchAsync(db, mission, mission.AgentId, run.AttemptNumber + 1, ct);
            if (outcome == MissionDispatchOutcome.NoLiveConnection)
            {
                // Park instead of failing — AgentReconnectedMissionWakeConsumer will retry when a
                // covering agent comes back online (the runner's XWaitingForRunner analogue).
                mission.Status = MissionStatus.WaitingForAgent;
                await db.SaveChangesAsync(ct);
            }
            else if (outcome == MissionDispatchOutcome.AgentNotAssigned)
            {
                // Distinct status from WaitingForAgent: the agent isn't offline, the assignment is missing.
                // The wake path that picks this up is a future follow-up (an Agent{Scope}Assignment-created
                // event consumer that re-attempts dispatch for parked missions referencing the affected Agent).
                mission.Status = MissionStatus.BlockedAgentNotAssigned;
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            _logger.LogWarning("Run {RunId} timed out; mission {MissionType} failed after {Max} attempts.",
                run.Id, run.MissionType, MaxAttempts);
            mission.Status = MissionStatus.Failed;
            mission.Error = run.Error;
            await db.SaveChangesAsync(ct);
        }
    }
}
