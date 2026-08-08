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
/// Handles completion, cancellation, and fault notifications from runners when they finish destroying from plan.
/// </summary>
public class DestroyFromPlanHandler
{
    private readonly ILogger<DestroyFromPlanHandler> _logger;
    private readonly IBus _bus;

    public DestroyFromPlanHandler(
        ILogger<DestroyFromPlanHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, int? actualResourceCount)
    {
        try
        {
            _logger.LogInformation("Runner completed DestroyFromPlan for job {JobId}", jobId);

            await _bus.Publish(new DestroyFromPlanCompleted
            {
                CorrelationId = jobId,
                ActualResourceCount = actualResourceCount
            });

            _logger.LogDebug("Sent DestroyFromPlan completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DestroyFromPlan completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled DestroyFromPlan for job {JobId}", jobId);

            await _bus.Publish(new DestroyFromPlanCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent DestroyFromPlan cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DestroyFromPlan cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        try
        {
            _logger.LogError("Runner faulted DestroyFromPlan for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new DestroyFromPlanFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                ActualResourceCount = actualResourceCount,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent DestroyFromPlan fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DestroyFromPlan fault for job {JobId}", jobId);
            throw;
        }
    }
}
