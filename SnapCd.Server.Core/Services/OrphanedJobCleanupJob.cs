// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Services;

public class OrphanedJobCleanupJob
{
    private readonly OrphanedJobCleanupService _cleanupService;
    private readonly ManualModuleJobRepositoryFactory _manualModuleJobRepositoryFactory;
    private readonly IBus _bus;
    private readonly ILogger<OrphanedJobCleanupJob> _logger;

    public OrphanedJobCleanupJob(
        OrphanedJobCleanupService cleanupService,
        ManualModuleJobRepositoryFactory manualModuleJobRepositoryFactory,
        IBus bus,
        ILogger<OrphanedJobCleanupJob> logger)
    {
        _cleanupService = cleanupService;
        _manualModuleJobRepositoryFactory = manualModuleJobRepositoryFactory;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// A manual job has no saga to route a cancellation through once its saga is gone, so the row
    /// is closed here directly. Leaving it open would block every future manual job on the Module
    /// through the filtered unique index.
    /// </summary>
    private async Task CleanUpManualJobs()
    {
        var orphaned = await _cleanupService.ListOrphanedManualJobs();

        foreach (var job in orphaned)
        {
            _logger.LogWarning(
                "Found orphaned manual {JobType} job {JobId} in organization {OrganizationId}, closing it",
                job.JobType, job.Id, job.OrganizationId);

            using var repository = _manualModuleJobRepositoryFactory.Create();
            await repository.FinalizeWithServerError(
                job.Id,
                job.OrganizationId,
                DateTimeOffset.UtcNow,
                null,
                "This job was abandoned.",
                "The job's saga is no longer present, so it could not be completed. It has been closed so that further manual jobs can run on this module.");
        }
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
            await CleanUpManualJobs();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned job cleanup");
        }
    }
}
