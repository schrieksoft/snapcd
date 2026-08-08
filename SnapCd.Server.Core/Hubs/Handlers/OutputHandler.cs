// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish output.
/// Database work is offloaded to OutputCompletedInvokedConsumer to avoid blocking SignalR.
/// </summary>
public class OutputHandler
{
    private readonly ILogger<OutputHandler> _logger;
    private readonly IBus _bus;

    public OutputHandler(
        ILogger<OutputHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, OutputSetCreateDto? outputSet)
    {
        try
        {
            _logger.LogInformation("Runner completed Output for job {JobId}", jobId);

            // Publish to consumer for database work (idempotency handled there)
            await _bus.Publish(new OutputCompletedInvoked
            {
                JobId = jobId,
                OutputSet = outputSet
            });


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Output completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Output for job {JobId}", jobId);

            await _bus.Publish(new OutputCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Output cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Output cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted Output for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new OutputFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Output fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Output fault for job {JobId}", jobId);
            throw;
        }
    }
}