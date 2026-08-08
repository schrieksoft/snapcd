// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish Variables collection.
/// Database work is offloaded to VariablesCompletedInvokedConsumer to avoid blocking SignalR.
/// </summary>
public class VariableHandler
{
    private readonly ILogger<VariableHandler> _logger;
    private readonly IBus _bus;

    public VariableHandler(
        ILogger<VariableHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, VariableSetCreateDto? variableSet)
    {
        try
        {
            _logger.LogInformation("Runner completed Variables for job {JobId}", jobId);

            // Publish to consumer for database work (idempotency handled there)
            await _bus.Publish(new VariablesCompletedInvoked
            {
                JobId = jobId,
                VariableSet = variableSet
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Variables completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Variables for job {JobId}", jobId);

            await _bus.Publish(new VariablesCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Variables cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Variables cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted Variables for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new VariablesFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Variables fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Variables fault for job {JobId}", jobId);
            throw;
        }
    }
}
