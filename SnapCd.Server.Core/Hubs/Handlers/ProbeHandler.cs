// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// What a runner reports when it finishes a probe. The signals are stored by
/// ApplyProbeCompletedInvokedConsumer rather than here, so SignalR is not held open for the write.
/// </summary>
public abstract class ProbeHandler<TInvoked, TCancelled, TFaulted>
    where TInvoked : ProbeCompletedInvokedBase, new()
    where TCancelled : StepResponseBase, new()
    where TFaulted : StepFaultedBase, new()
{
    private readonly ILogger _logger;
    private readonly IBus _bus;

    protected ProbeHandler(ILogger logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, string? signalsJson)
    {
        try
        {
            _logger.LogInformation("Runner completed Probe for job {JobId}", jobId);

            await _bus.Publish(new TInvoked
            {
                JobId = jobId,
                SignalsJson = signalsJson
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Probe completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId)
    {
        try
        {
            _logger.LogInformation("Runner cancelled Probe for job {JobId}", jobId);

            await _bus.Publish(new TCancelled { CorrelationId = jobId });

            _logger.LogDebug("Sent Probe cancellation response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Probe cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted Probe for job {JobId}: {ErrorMessage}", jobId, errorMessage);

            await _bus.Publish(new TFaulted
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                CorrelationId = jobId
            });

            _logger.LogDebug("Sent Probe fault response for job {JobId} to saga", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Probe fault for job {JobId}", jobId);
            throw;
        }
    }
}

public class ApplyProbeHandler(ILogger<ApplyProbeHandler> logger, IBus bus)
    : ProbeHandler<ApplyProbeCompletedInvoked, ApplyProbeCancelled, ApplyProbeFaulted>(logger, bus);
