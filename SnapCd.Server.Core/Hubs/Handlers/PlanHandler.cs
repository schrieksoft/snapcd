// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish planning.
/// </summary>
public class PlanHandler
{
    private readonly ILogger<PlanHandler> _logger;
    private readonly IBus _bus;

    public PlanHandler(
        ILogger<PlanHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, PlanCompletedData data)
    {
        try
        {
            _logger.LogInformation("Runner completed Plan for job {JobId}", jobId);

            await _bus.Publish(new PlanCompleted
            {
                CorrelationId = jobId,
                PolicyOutcome = data.PolicyOutcome,
                TotalCountAfter = data.TotalCountAfter,
                TotalCountBefore = data.TotalCountBefore,
                TotalChangedCount = data.TotalChangedCount,
                TotalUnchangedCount = data.TotalUnchangedCount,
                CreateCount = data.CreateCount,
                ModifyCount = data.ModifyCount,
                DestroyCount = data.DestroyCount,
                RecreateCount = data.RecreateCount,
                OutputsTotalCount = data.OutputsTotalCount,
                OutputsTotalChangedCount = data.OutputsTotalChangedCount,
                OutputsTotalUnchangedCount = data.OutputsTotalUnchangedCount,
                OutputsCreateCount = data.OutputsCreateCount,
                OutputsModifyCount = data.OutputsModifyCount,
                OutputsDestroyCount = data.OutputsDestroyCount,
                OutputsRecreateCount = data.OutputsRecreateCount,
                OutputsUnchangedList = data.OutputsUnchangedList != null ? string.Join(",", data.OutputsUnchangedList) : null,
                OutputsCreateList = data.OutputsCreateList != null ? string.Join(",", data.OutputsCreateList) : null,
                OutputsModifyList = data.OutputsModifyList != null ? string.Join(",", data.OutputsModifyList) : null,
                OutputsDestroyList = data.OutputsDestroyList != null ? string.Join(",", data.OutputsDestroyList) : null,
                OutputsRecreateList = data.OutputsRecreateList != null ? string.Join(",", data.OutputsRecreateList) : null
            });

            _logger.LogDebug("Sent Plan completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Plan completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Plan for job {JobId}", jobId);

            await _bus.Publish(new PlanCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Plan cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Plan cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        try
        {
            _logger.LogError("Runner faulted Plan for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new PlanFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId,
                PolicyOutcome = policyOutcome
            });

            _logger.LogDebug("Sent Plan fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Plan fault for job {JobId}", jobId);
            throw;
        }
    }
}
