// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;

namespace SnapCd.Server.Core.Hubs.Handlers.SplitMonolith;

public class RefactorVerifyHandler
{
    private readonly ILogger<RefactorVerifyHandler> _logger;
    private readonly IBus _bus;

    public RefactorVerifyHandler(ILogger<RefactorVerifyHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner completed RefactorVerify for job {JobId}", jobId);

            await _bus.Publish(new RefactorVerifyCompleted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RefactorVerify completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner cancelled RefactorVerify for job {JobId}", jobId);

            await _bus.Publish(new RefactorVerifyCancelled
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RefactorVerify cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted RefactorVerify for job {JobId}: {ErrorMessage}", jobId, errorMessage);

            await _bus.Publish(new RefactorVerifyFaulted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RefactorVerify fault for job {JobId}", jobId);
            throw;
        }
    }
}
