// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Answers one job family's heartbeat by reading when the runner last reported a running task
/// for the job. Which table holds that row differs per family, so each family closes this over
/// its own request type and its own query.
///
/// A stale row is judged on age. A missing row is judged on two heartbeats instead, because a
/// window measured in seconds can expire while no heartbeat is looking, leaving a job that was
/// read as healthy once and never reassessed.
/// </summary>
public abstract class HeartbeatConsumerBase<TRequest> : IConsumer<TRequest>
    where TRequest : HeartbeatRequestedBase
{
    private readonly ILogger _logger;
    private readonly SnapCdDbContext _dbContext;

    protected HeartbeatConsumerBase(ILogger logger, SnapCdDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>How stale a report may be before the job is given up on.</summary>
    private const int ThresholdSeconds = 90;

    /// <summary>When the runner last reported on this job, or null if it never has.</summary>
    protected abstract Task<DateTime?> LastReportedAt(SnapCdDbContext _dbContext, Guid organizationId, Guid jobId);

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        var correlationId = context.Message.CorrelationId;
        var orgId = context.Message.OrganizationId;

        _logger.LogDebug("Received heartbeat request for job {CorrelationId}", correlationId);

        try
        {

            var lastReportedAt = await LastReportedAt(_dbContext, orgId, correlationId);

            if (lastReportedAt == null)
            {
                // A reconnect deletes the connection's job rows; the runner's next report recreates them.
                var reconnectedAt = await _dbContext.RunnerConnections
                    .Where(rc => rc.OrganizationId == orgId
                                 && rc.RunnerId == context.Message.RunnerId
                                 && rc.InstanceName == context.Message.RunnerInstanceName)
                    .Select(rc => (DateTime?)rc.CreatedDateTime)
                    .FirstOrDefaultAsync();

                // A connected runner gets one heartbeat to produce a report. The grace is counted
                // in heartbeats, so it is only ever spent by a heartbeat that observes it.
                if (reconnectedAt != null && !context.Message.MissedHeartbeat)
                {
                    _logger.LogDebug(
                        "Job {CorrelationId} has no runner connection record; runner instance {Instance} is connected, so one heartbeat is allowed for it to report",
                        correlationId, context.Message.RunnerInstanceName);

                    await context.RespondAsync(new HeartbeatCompleted { MissedHeartbeat = true });
                    return;
                }

                var reason = reconnectedAt == null
                    ? $"Runner instance '{context.Message.RunnerInstanceName}' is not connected."
                    : $"Runner instance '{context.Message.RunnerInstanceName}' is connected but has not reported on this job for two heartbeats.";

                _logger.LogWarning("Job {CorrelationId} heartbeat failed: {Reason}", correlationId, reason);
                await context.RespondAsync(new HeartbeatFailed { CorrelationId = correlationId, Reason = reason });
                return;
            }

            var age = DateTime.UtcNow - lastReportedAt.Value;

            if (age.TotalSeconds > ThresholdSeconds)
            {
                _logger.LogWarning("Job {CorrelationId} heartbeat failed - last update was {Age:F1} seconds ago (threshold: {Threshold}s)",
                    correlationId, age.TotalSeconds, ThresholdSeconds);
                await context.RespondAsync(new HeartbeatFailed { CorrelationId = correlationId });
            }
            else
            {
                _logger.LogDebug("Job {CorrelationId} heartbeat successful - last update was {Age:F1} seconds ago",
                    correlationId, age.TotalSeconds);

                await context.RespondAsync(new HeartbeatCompleted());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking heartbeat for job {CorrelationId}", correlationId);
            await context.RespondAsync(new HeartbeatFailed { CorrelationId = correlationId });
        }
    }
}

public class ModuleJobHeartbeatConsumer : HeartbeatConsumerBase<ModuleJobHeartbeatRequested>
{
    public ModuleJobHeartbeatConsumer(ILogger<ModuleJobHeartbeatConsumer> logger, SnapCdDbContext dbContext)
        : base(logger, dbContext) { }

    protected override async Task<DateTime?> LastReportedAt(SnapCdDbContext _dbContext, Guid organizationId, Guid jobId) =>
        await _dbContext.RunnerConnectionJobs
            .Where(r => r.OrganizationId == organizationId && r.ModuleJobId == jobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
}

public class StateMigrationHeartbeatConsumer : HeartbeatConsumerBase<StateMigrationHeartbeatRequested>
{
    public StateMigrationHeartbeatConsumer(ILogger<StateMigrationHeartbeatConsumer> logger, SnapCdDbContext dbContext)
        : base(logger, dbContext) { }

    protected override async Task<DateTime?> LastReportedAt(SnapCdDbContext _dbContext, Guid organizationId, Guid jobId) =>
        await _dbContext.RunnerConnectionStateMigrations
            .Where(r => r.OrganizationId == organizationId && r.StateMigrationJobId == jobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
}
