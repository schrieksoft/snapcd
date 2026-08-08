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
/// Handles graceful cancellation completion notifications from runners.
/// </summary>
public class CancelGracefulHandler
{
    private readonly ILogger<CancelGracefulHandler> _logger;
    private readonly IBus _bus;

    public CancelGracefulHandler(
        ILogger<CancelGracefulHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner completed graceful cancellation for job {JobId}", jobId);

            await _bus.Publish(new CancelGracefulCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent graceful cancellation completion response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing graceful cancellation completion for job {JobId}", jobId);
            throw;
        }
    }
}
