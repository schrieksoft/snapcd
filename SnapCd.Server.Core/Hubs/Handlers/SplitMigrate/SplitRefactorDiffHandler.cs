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

public class SplitRefactorDiffHandler
{
    private readonly ILogger<SplitRefactorDiffHandler> _logger;
    private readonly IBus _bus;

    public SplitRefactorDiffHandler(ILogger<SplitRefactorDiffHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner completed SplitRefactorDiff for job {JobId}", jobId);

            await _bus.Publish(new SplitRefactorDiffCompleted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitRefactorDiff completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner cancelled SplitRefactorDiff for job {JobId}", jobId);

            await _bus.Publish(new SplitRefactorDiffCancelled
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitRefactorDiff cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted SplitRefactorDiff for job {JobId}: {ErrorMessage}", jobId, errorMessage);

            await _bus.Publish(new SplitRefactorDiffFaulted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitRefactorDiff fault for job {JobId}", jobId);
            throw;
        }
    }
}
