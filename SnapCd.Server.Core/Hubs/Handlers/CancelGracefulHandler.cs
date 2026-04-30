using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles graceful cancellation completion notifications from runners.
/// </summary>
public class CancelGracefulHandler
{
    private readonly ILogger<CancelGracefulHandler> _logger;
    private readonly IBus _bus;

    public CancelGracefulHandler(
        ILogger<CancelGracefulHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed graceful cancellation for job {JobId}", jobId);

            await _bus.Publish(new CancelGracefulCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent graceful cancellation completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing graceful cancellation completion for job {JobId}", jobId);
            throw;
        }
    }
}
