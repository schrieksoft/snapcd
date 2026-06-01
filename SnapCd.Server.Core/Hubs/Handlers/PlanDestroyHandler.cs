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

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles completion, cancellation, and fault notifications from runners when they finish destroy planning.
/// </summary>
public class PlanDestroyHandler
{
    private readonly ILogger<PlanDestroyHandler> _logger;
    private readonly IBus _bus;

    public PlanDestroyHandler(
        ILogger<PlanDestroyHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, PlanCompletedData data)
    {
        try
        {
            _logger.LogInformation("Runner completed PlanDestroy for job {JobId}", jobId);

            await _bus.Publish(new PlanDestroyCompleted
            {
                CorrelationId = jobId,
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

            _logger.LogInformation("Sent PlanDestroy completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PlanDestroy completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled PlanDestroy for job {JobId}", jobId);

            await _bus.Publish(new PlanDestroyCancelled
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent PlanDestroy cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PlanDestroy cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted PlanDestroy for job {JobId}: {ErrorMessage}",
                jobId, errorMessage);

            await _bus.Publish(new PlanDestroyFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogInformation("Sent PlanDestroy fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PlanDestroy fault for job {JobId}", jobId);
            throw;
        }
    }
}
