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
