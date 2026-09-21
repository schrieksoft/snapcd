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

/// <summary>Relays the runner's Validate result for a split job back to its saga.</summary>
public class SplitValidateHandler
{
    private readonly ILogger<SplitValidateHandler> _logger;
    private readonly IBus _bus;

    public SplitValidateHandler(ILogger<SplitValidateHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, Guid organizationId)
    {
        _logger.LogDebug("Runner completed Validate for split job {JobId}", jobId);
        await _bus.Publish(new SplitValidateCompleted { CorrelationId = jobId, OrganizationId = organizationId });
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        _logger.LogDebug("Runner cancelled Validate for split job {JobId}", jobId);
        await _bus.Publish(new SplitValidateCancelled { CorrelationId = jobId, OrganizationId = organizationId });
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        _logger.LogWarning("Runner faulted Validate for split job {JobId}: {Error}", jobId, errorMessage);
        await _bus.Publish(new SplitValidateFaulted
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }
}
