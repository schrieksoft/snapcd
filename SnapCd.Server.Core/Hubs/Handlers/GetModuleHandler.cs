using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish getting the module.
/// </summary>
public class GetModuleHandler
{
    private readonly ILogger<GetModuleHandler> _logger;
    private readonly IBus _bus;

    public GetModuleHandler(
        ILogger<GetModuleHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed GetModule for job {JobId}", jobId);

            await _bus.Publish(new GetModuleCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetModule completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetModule completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled GetModule for job {JobId}", jobId);

            await _bus.Publish(new GetModuleCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetModule cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetModule cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted GetModule for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new GetModuleFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetModule fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetModule fault for job {JobId}", jobId);
            throw;
        }
    }
}
