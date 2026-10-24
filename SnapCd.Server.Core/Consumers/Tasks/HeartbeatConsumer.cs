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
/// Server-side consumer that receives heartbeat requests and checks if the job's RunnerConnectionJob
/// record has been updated recently (within 90 seconds).
/// </summary>
public class HeartbeatConsumer : IConsumer<HeartbeatRequested>
{
    private const int ThresholdSeconds = 90;

    private readonly ILogger<HeartbeatConsumer> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public HeartbeatConsumer(
        ILogger<HeartbeatConsumer> logger,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Consume(ConsumeContext<HeartbeatRequested> context)
    {
        var msg = context.Message;
        var correlationId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        _logger.LogDebug("Received heartbeat request for job {CorrelationId}", correlationId);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var lastReportedAt = msg.IsStateMigration
                ? await dbContext.RunnerConnectionStateMigrations
                    .Where(r => r.OrganizationId == orgId && r.StateMigrationJobId == correlationId)
                    .Select(r => (DateTime?)r.ModifiedDateTime)
                    .FirstOrDefaultAsync()
                : await dbContext.RunnerConnectionJobs
                    .Where(rcj => rcj.OrganizationId == orgId && rcj.ModuleJobId == correlationId)
                    .Select(rcj => (DateTime?)rcj.ModifiedDateTime)
                    .FirstOrDefaultAsync();

            if (lastReportedAt == null)
            {
                // A reconnect deletes the connection's job rows; the runner's next report recreates them.
                var reconnectedAt = await dbContext.RunnerConnections
                    .Where(rc => rc.OrganizationId == orgId && rc.RunnerId == msg.RunnerId && rc.InstanceName == msg.RunnerInstanceName)
                    .Select(rc => (DateTime?)rc.CreatedDateTime)
                    .FirstOrDefaultAsync();

                if (reconnectedAt != null && (DateTime.UtcNow - reconnectedAt.Value).TotalSeconds <= ThresholdSeconds)
                {
                    _logger.LogDebug("Job {CorrelationId} has no runner connection record, but runner instance {Instance} reconnected {Age:F1} seconds ago",
                        correlationId, msg.RunnerInstanceName, (DateTime.UtcNow - reconnectedAt.Value).TotalSeconds);
                    await context.RespondAsync(new HeartbeatCompleted());
                    return;
                }

                var reason = reconnectedAt == null
                    ? $"Runner instance '{msg.RunnerInstanceName}' is not connected."
                    : $"Runner instance '{msg.RunnerInstanceName}' reconnected at {reconnectedAt.Value:u} but has not reported on this job since.";

                _logger.LogWarning("No runner connection record found for job {CorrelationId}. {Reason}", correlationId, reason);
                await context.RespondAsync(new HeartbeatFailed
                {
                    CorrelationId = correlationId,
                    Reason = reason
                });
                return;
            }

            var age = DateTime.UtcNow - lastReportedAt.Value;

            if (age.TotalSeconds > ThresholdSeconds)
            {
                _logger.LogWarning("Job {CorrelationId} heartbeat failed - last update was {Age:F1} seconds ago (threshold: {Threshold}s)",
                    correlationId, age.TotalSeconds, ThresholdSeconds);
                await context.RespondAsync(new HeartbeatFailed
                {
                    CorrelationId = correlationId,
                    Reason = $"The runner last reported on this job at {lastReportedAt.Value:u}, {age.TotalSeconds:F0} seconds before the check."
                });
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
            await context.RespondAsync(new HeartbeatFailed
            {
                CorrelationId = correlationId,
                Reason = $"The server could not check the runner's liveness: {ex.Message}"
            });
        }
    }
}