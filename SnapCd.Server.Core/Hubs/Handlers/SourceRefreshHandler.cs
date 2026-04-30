using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion and fault notifications from runners for source refresh operations.
/// This is a stateless handler - no request tracking needed since responses are matched by source parameters.
/// </summary>
public class SourceRefreshHandler
{
    private readonly ILogger<SourceRefreshHandler> _logger;
    private readonly IBus _bus;

    public SourceRefreshHandler(
        ILogger<SourceRefreshHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(string sourceUrl, string sourceRevision, SourceType sourceType, SourceRevisionType sourceRevisionType, string definitiveRevision)
    {
        try
        {
            _logger.LogInformation("Runner completed source refresh for {SourceUrl} @ {SourceRevision}", sourceUrl, sourceRevision);

            // Publish event directly - no tracking needed
            // SourceRefreshCompletedCompetingConsumer will match by source parameters
            await _bus.Publish(new SourceRefreshCompleted
            {
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceType = sourceType,
                SourceRevisionType = sourceRevisionType,
                DefinitiveRevision = definitiveRevision
            });

            _logger.LogInformation("Published SourceRefreshCompleted for {SourceUrl} @ {SourceRevision} -> {DefinitiveRevision}",
                sourceUrl, sourceRevision, definitiveRevision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing source refresh completion for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
            throw;
        }
    }

    public async Task Fault(string sourceUrl, string sourceRevision, SourceType sourceType, SourceRevisionType sourceRevisionType, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted source refresh for {SourceUrl} @ {SourceRevision}: {ErrorMessage}",
                sourceUrl, sourceRevision, errorMessage);

            // Publish fault event directly - no tracking needed
            await _bus.Publish(new SourceRefreshFaulted
            {
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceType = sourceType,
                SourceRevisionType = sourceRevisionType,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });

            _logger.LogInformation("Published SourceRefreshFaulted for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing source refresh fault for {SourceUrl} @ {SourceRevision}",
                sourceUrl, sourceRevision);
            throw;
        }
    }
}
