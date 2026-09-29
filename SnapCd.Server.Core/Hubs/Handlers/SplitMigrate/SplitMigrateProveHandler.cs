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

public class SplitMigrateProveHandler
{
    private readonly ILogger<SplitMigrateProveHandler> _logger;
    private readonly IBus _bus;

    public SplitMigrateProveHandler(ILogger<SplitMigrateProveHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Complete(Guid jobId, Guid organizationId, int modulesProven, int modulesPlanningClean)
    {
        try
        {
            _logger.LogDebug("Runner completed SplitMigrateProve for job {JobId}", jobId);

            await _bus.Publish(new SplitMigrateProveCompleted
            {
                ModulesProven = modulesProven,
                ModulesPlanningClean = modulesPlanningClean,
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitMigrateProve completion for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Cancel(Guid jobId, Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Runner cancelled SplitMigrateProve for job {JobId}", jobId);

            await _bus.Publish(new SplitMigrateProveCancelled
            {
                CorrelationId = jobId,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitMigrateProve cancellation for job {JobId}", jobId);
            throw;
        }
    }

    public async Task Fault(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
    {
        try
        {
            _logger.LogError("Runner faulted SplitMigrateProve for job {JobId}: {ErrorMessage}", jobId, errorMessage);

            await _bus.Publish(new SplitMigrateProveFaulted
            {
                CorrelationId = jobId,
                OrganizationId = organizationId,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SplitMigrateProve fault for job {JobId}", jobId);
            throw;
        }
    }
}
