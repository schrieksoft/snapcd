// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish policy validation.
/// </summary>
public class PolicyValidateHandler
{
    private readonly ILogger<PolicyValidateHandler> _logger;
    private readonly IBus _bus;

    public PolicyValidateHandler(
        ILogger<PolicyValidateHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, PolicyOutcome outcome)
    {
        try
        {
            _logger.LogInformation("Runner completed PolicyValidate for job {JobId} with outcome {Outcome}", jobId, outcome);

            await _bus.Publish(new PolicyValidateCompleted
            {
                CorrelationId = jobId,
                Outcome = outcome
            });

            _logger.LogDebug("Sent PolicyValidate completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PolicyValidate completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled PolicyValidate for job {JobId}", jobId);

            await _bus.Publish(new PolicyValidateCancelled
            {
                CorrelationId = jobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PolicyValidate cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogWarning("Runner faulted PolicyValidate for job {JobId}: {Error}", jobId, errorMessage);

            await _bus.Publish(new PolicyValidateFaulted
            {
                CorrelationId = jobId,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PolicyValidate fault for job {JobId}", jobId);
            throw;
        }
    }
}
