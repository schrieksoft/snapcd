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
using SnapCd.Server.Core.Services.Nonsecured.Ai.Missions;

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

    private readonly SnapCdDbContext _dbContext;
    private readonly MissionDispatcher _dispatcher;
    private readonly IMessageScheduler _scheduler;
    private readonly ILogger<MissionRunDeadlineCheckConsumer> _logger;

    public MissionRunDeadlineCheckConsumer(
        SnapCdDbContext dbContext,
        MissionDispatcher dispatcher,
        IMessageScheduler scheduler,
        ILogger<MissionRunDeadlineCheckConsumer> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MissionRunDeadlineCheck> context)
    {
        var ct = context.CancellationToken;

        var run = await _dbContext.ModuleJobMissionRuns
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
        await _dbContext.SaveChangesAsync(ct); // frees the filtered-unique lock before any re-claim

        var mission = await _dbContext.ModuleJobMissions
            .FirstOrDefaultAsync(m => m.Id == run.ModuleJobMissionId && m.OrganizationId == run.OrganizationId, ct);
        if (mission is null)
            return;

        if (run.AttemptNumber < MaxAttempts)
        {
            _logger.LogWarning("Run {RunId} timed out; retrying mission {MissionType} (attempt {Next}/{Max}).",
                run.Id, run.MissionType, run.AttemptNumber + 1, MaxAttempts);
            var outcome = await _dispatcher.DispatchAsync(_dbContext, mission, mission.AgentId, run.AttemptNumber + 1, ct);
            if (outcome is MissionDispatchOutcome.NoLiveConnection
                or MissionDispatchOutcome.AgentNotAssigned)
            {
                mission.Status = MissionStatus.Failed;
                mission.Error = outcome == MissionDispatchOutcome.NoLiveConnection
                    ? "No agent covering this Module was connected when the mission was retried."
                    : "No Agent assignment covers this Module, so no agent could run the mission.";
                mission.CompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Mission {MissionType} (job {JobId}) failed: {Error}",
                    mission.MissionType, mission.ModuleJobId, mission.Error);
            }
        }
        else
        {
            _logger.LogWarning("Run {RunId} timed out; mission {MissionType} failed after {Max} attempts.",
                run.Id, run.MissionType, MaxAttempts);
            mission.Status = MissionStatus.Failed;
            mission.Error = run.Error;
            mission.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
