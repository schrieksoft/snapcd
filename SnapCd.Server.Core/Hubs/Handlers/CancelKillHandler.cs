using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles kill cancellation completion notifications from runners.
/// </summary>
public class CancelKillHandler
{
    private readonly ILogger<CancelKillHandler> _logger;
    private readonly IBus _bus;

    public CancelKillHandler(
        ILogger<CancelKillHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed kill cancellation for job {JobId}", jobId);

            await _bus.Publish(new CancelKillCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent kill cancellation completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing kill cancellation completion for job {JobId}", jobId);
            throw;
        }
    }
}
