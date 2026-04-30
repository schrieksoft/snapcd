using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish applying from plan.
/// </summary>
public class ApplyFromPlanHandler
{
    private readonly ILogger<ApplyFromPlanHandler> _logger;
    private readonly IBus _bus;

    public ApplyFromPlanHandler(
        ILogger<ApplyFromPlanHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, int? actualResourceCount)
    {
        try
        {
            _logger.LogInformation("Runner completed ApplyFromPlan for job {JobId} with resource count {ResourceCount}",
                jobId, actualResourceCount);

            await _bus.Publish(new ApplyFromPlanCompleted
            {
                CorrelationId = jobId,
                ActualResourceCount = actualResourceCount
            });

            _logger.LogInformation("Sent ApplyFromPlan completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ApplyFromPlan completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled ApplyFromPlan for job {JobId}", jobId);

            await _bus.Publish(new ApplyFromPlanCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent ApplyFromPlan cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ApplyFromPlan cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        try
        {
            _logger.LogError("Runner faulted ApplyFromPlan for job {JobId}: {ErrorMessage}, ResourceCount: {ResourceCount}",
                jobId, errorMessage, actualResourceCount);

            await _bus.Publish(new ApplyFromPlanFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                ActualResourceCount = actualResourceCount,
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent ApplyFromPlan fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ApplyFromPlan fault for job {JobId}", jobId);
            throw;
        }
    }
}
