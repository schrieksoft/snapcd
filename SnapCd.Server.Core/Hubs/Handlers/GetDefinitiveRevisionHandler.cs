// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish getting the definitive module revision.
/// </summary>
public class GetDefinitiveRevisionHandler
{
    private readonly ILogger<GetDefinitiveRevisionHandler> _logger;
    private readonly IBus _bus;

    public GetDefinitiveRevisionHandler(
        ILogger<GetDefinitiveRevisionHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, string definitiveRevision)
    {
        try
        {
            _logger.LogInformation("Runner completed GetDefinitiveRevision for job {JobId}", jobId);

            await _bus.Publish(new GetDefinitiveRevisionCompleted
            {
                DefinitiveRevision = definitiveRevision,
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetDefinitiveRevision completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetDefinitiveRevision completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled GetDefinitiveRevision for job {JobId}", jobId);

            await _bus.Publish(new GetDefinitiveRevisionCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetDefinitiveRevision cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetDefinitiveRevision cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted GetDefinitiveRevision for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new GetDefinitiveRevisionFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent GetDefinitiveRevision fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetDefinitiveRevision fault for job {JobId}", jobId);
            throw;
        }
    }
}
