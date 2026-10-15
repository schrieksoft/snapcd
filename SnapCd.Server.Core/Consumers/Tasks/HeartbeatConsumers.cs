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
/// for the job. The 90 second threshold is the same for every family; which table holds the row
/// is not, so each family closes this over its own request type and its own query.
/// </summary>
public abstract class HeartbeatConsumerBase<TRequest> : IConsumer<TRequest>
    where TRequest : HeartbeatRequestedBase
{
    private readonly ILogger _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    protected HeartbeatConsumerBase(ILogger logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>When the runner last reported on this job, or null if it never has.</summary>
    protected abstract Task<DateTime?> LastReportedAt(SnapCdDbContext dbContext, Guid organizationId, Guid jobId);

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        var correlationId = context.Message.CorrelationId;
        var orgId = context.Message.OrganizationId;

        _logger.LogDebug("Received heartbeat request for job {CorrelationId}", correlationId);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var lastReportedAt = await LastReportedAt(dbContext, orgId, correlationId);

            if (lastReportedAt == null)
            {
                _logger.LogWarning("No runner connection record found for job {CorrelationId}", correlationId);
                await context.RespondAsync(new HeartbeatFailed { CorrelationId = correlationId });
                return;
            }

            var age = DateTime.UtcNow - lastReportedAt.Value;

            if (age.TotalSeconds > 90)
            {
                _logger.LogWarning("Job {CorrelationId} heartbeat failed - last update was {Age:F1} seconds ago (threshold: 90s)",
                    correlationId, age.TotalSeconds);
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
    public ModuleJobHeartbeatConsumer(ILogger<ModuleJobHeartbeatConsumer> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
        : base(logger, dbContextFactory) { }

    protected override async Task<DateTime?> LastReportedAt(SnapCdDbContext dbContext, Guid organizationId, Guid jobId) =>
        await dbContext.RunnerConnectionJobs
            .Where(r => r.OrganizationId == organizationId && r.ModuleJobId == jobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
}

public class StateMigrationHeartbeatConsumer : HeartbeatConsumerBase<StateMigrationHeartbeatRequested>
{
    public StateMigrationHeartbeatConsumer(ILogger<StateMigrationHeartbeatConsumer> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
        : base(logger, dbContextFactory) { }

    protected override async Task<DateTime?> LastReportedAt(SnapCdDbContext dbContext, Guid organizationId, Guid jobId) =>
        await dbContext.RunnerConnectionStateMigrations
            .Where(r => r.OrganizationId == organizationId && r.StateMigrationJobId == jobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
}

public class RippleProbeHeartbeatConsumer : HeartbeatConsumerBase<RippleProbeHeartbeatRequested>
{
    public RippleProbeHeartbeatConsumer(ILogger<RippleProbeHeartbeatConsumer> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
        : base(logger, dbContextFactory) { }

    protected override async Task<DateTime?> LastReportedAt(SnapCdDbContext dbContext, Guid organizationId, Guid jobId) =>
        await dbContext.RunnerConnectionRippleProbes
            .Where(r => r.OrganizationId == organizationId && r.RippleProbeJobId == jobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
}
