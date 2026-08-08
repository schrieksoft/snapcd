// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Services;

public class OrphanedJobCleanupJob
{
    private readonly OrphanedJobCleanupService _cleanupService;
    private readonly IBus _bus;
    private readonly ILogger<OrphanedJobCleanupJob> _logger;

    public OrphanedJobCleanupJob(
        OrphanedJobCleanupService cleanupService,
        IBus bus,
        ILogger<OrphanedJobCleanupJob> logger)
    {
        _cleanupService = cleanupService;
        _bus = bus;
        _logger = logger;
    }

    public async Task ExecuteJob()
    {
        using var _ = SnapCd.Server.Core.Services.CallerContext.CallerContext.Begin(SnapCd.Server.Core.Services.CallerContext.CallerKind.System);
        try
        {
            var orphanedJobs = await _cleanupService.ListOrphanedJobs();

            foreach (var job in orphanedJobs)
            {
                _logger.LogWarning(
                    "Found orphaned {JobType} job {JobId} in organization {OrganizationId}, publishing Kill cancellation",
                    job.JobType, job.Id, job.OrganizationId);

                await _bus.Publish(new CleanupOrphanedJobRequested
                {
                    CorrelationId = job.Id,
                    OrganizationId = job.OrganizationId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned job cleanup");
        }
    }
}
