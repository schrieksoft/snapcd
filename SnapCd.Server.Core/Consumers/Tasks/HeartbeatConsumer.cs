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

        _logger.LogInformation("Received heartbeat request for job {CorrelationId}", correlationId);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Look up the RunnerConnectionJob for this job
            var runnerConnectionJob = await dbContext.RunnerConnectionJobs
                .Where(rcj => rcj.OrganizationId == orgId && rcj.ModuleJobId == correlationId)
                .Select(rcj => new { rcj.ModifiedDateTime })
                .FirstOrDefaultAsync();

            if (runnerConnectionJob == null)
            {
                _logger.LogWarning("No RunnerConnectionJob found for job {CorrelationId}", correlationId);
                await context.RespondAsync(new HeartbeatFailed
                {
                    CorrelationId = correlationId
                });
                return;
            }

            // Check if ModifiedDateTime is more than 90 seconds old
            var age = DateTime.UtcNow - runnerConnectionJob.ModifiedDateTime;

            if (age.TotalSeconds > 90)
            {
                _logger.LogWarning("Job {CorrelationId} heartbeat failed - last update was {Age:F1} seconds ago (threshold: 90s)",
                    correlationId, age.TotalSeconds);
                await context.RespondAsync(new HeartbeatFailed
                {
                    CorrelationId = correlationId
                });
            }
            else
            {
                _logger.LogInformation("Job {CorrelationId} heartbeat successful - last update was {Age:F1} seconds ago",
                    correlationId, age.TotalSeconds);
                await context.RespondAsync(new HeartbeatCompleted());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking heartbeat for job {CorrelationId}", correlationId);
            await context.RespondAsync(new HeartbeatFailed
            {
                CorrelationId = correlationId
            });
        }
    }
}