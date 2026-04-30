using MassTransit;
using SnapCd.Server.Core.Events.Handlers;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles running task reports from runners.
/// Database work is offloaded to ReportRunningTaskInvokedConsumer to avoid blocking SignalR.
/// </summary>
public class ReportRunningTaskHandler
{
    private readonly ILogger<ReportRunningTaskHandler> _logger;
    private readonly IBus _bus;

    public ReportRunningTaskHandler(
        ILogger<ReportRunningTaskHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Report(Guid organizationId, Guid jobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        try
        {
            _logger.LogInformation("Runner reported running task {TaskName} for job {JobId}", taskName, jobId);

            // Publish to consumer for database work (idempotency handled there)
            await _bus.Publish(new ReportRunningTaskInvoked
            {
                OrganizationId = organizationId,
                JobId = jobId,
                TaskName = taskName,
                RunnerId = runnerId,
                RunnerInstanceName = runnerInstanceName
            });

            _logger.LogInformation("Published ReportRunningTask event for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReportRunningTask for job {JobId}", jobId);
            throw;
        }
    }
}
