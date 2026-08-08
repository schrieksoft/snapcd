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
/// Handles completion, cancellation, and fault notifications from runners when they finish initialization.
/// </summary>
public class InitHandler
{
    private readonly ILogger<InitHandler> _logger;
    private readonly IBus _bus;

    public InitHandler(
        ILogger<InitHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed Init for job {JobId}", jobId);

            await _bus.Publish(new InitCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Init completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Init completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Init for job {JobId}", jobId);

            await _bus.Publish(new InitCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Init cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Init cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted Init for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new InitFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Init fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Init fault for job {JobId}", jobId);
            throw;
        }
    }
}
