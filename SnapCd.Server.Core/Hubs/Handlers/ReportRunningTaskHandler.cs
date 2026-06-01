// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Handlers;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Handles running task reports from runners.
/// Database work is offloaded to ReportRunningTaskInvokedConsumer to avoid blocking SignalR.
/// </summary>
public class ReportRunningTaskHandler
{
    private readonly ILogger<ReportRunningTaskHandler> _logger;
    private readonly IBus _bus;

    public ReportRunningTaskHandler(
        ILogger<ReportRunningTaskHandler> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    public async Task Report(Guid organizationId, Guid jobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        try
        {
            _logger.LogInformation("Runner reported running task {TaskName} for job {JobId}", taskName, jobId);

            // Publish to consumer for database work (idempotency handled there)
            await _bus.Publish(new ReportRunningTaskInvoked
            {
                OrganizationId = organizationId,
                JobId = jobId,
                TaskName = taskName,
                RunnerId = runnerId,
                RunnerInstanceName = runnerInstanceName
            });

            _logger.LogInformation("Published ReportRunningTask event for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReportRunningTask for job {JobId}", jobId);
            throw;
        }
    }
}
