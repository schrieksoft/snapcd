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
/// Handles completion, cancellation, and fault notifications from runners when they finish validation.
/// </summary>
public class ValidateHandler
{
    private readonly ILogger<ValidateHandler> _logger;
    private readonly IBus _bus;

    public ValidateHandler(
        ILogger<ValidateHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed Validate for job {JobId}", jobId);

            await _bus.Publish(new ValidateCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Validate completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Validate completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Validate for job {JobId}", jobId);

            await _bus.Publish(new ValidateCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Validate cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Validate cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted Validate for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new ValidateFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Validate fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Validate fault for job {JobId}", jobId);
            throw;
        }
    }
}
