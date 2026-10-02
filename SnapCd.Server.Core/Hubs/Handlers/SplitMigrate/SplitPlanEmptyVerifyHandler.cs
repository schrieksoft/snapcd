// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;

namespace SnapCd.Server.Core.Hubs.Handlers.SplitMigrate;

/// <summary>
/// Relays the runner's verdict on whether the module's plan was empty back to the saga.
/// </summary>
public class SplitPlanEmptyVerifyHandler
{
    private readonly ILogger<SplitPlanEmptyVerifyHandler> _logger;
    private readonly IBus _bus;

    public SplitPlanEmptyVerifyHandler(ILogger<SplitPlanEmptyVerifyHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner completed SplitPlanEmptyVerify for job {JobId}", jobId);

            await _bus.Publish(new SplitPlanEmptyVerifyCompleted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitPlanEmptyVerify completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner cancelled SplitPlanEmptyVerify for job {JobId}", jobId);

            await _bus.Publish(new SplitPlanEmptyVerifyCancelled
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitPlanEmptyVerify cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted SplitPlanEmptyVerify for job {JobId}: {ErrorMessage}", jobId, errorMessage);

            await _bus.Publish(new SplitPlanEmptyVerifyFaulted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitPlanEmptyVerify fault for job {JobId}", jobId);
            throw;
        }
    }
}
