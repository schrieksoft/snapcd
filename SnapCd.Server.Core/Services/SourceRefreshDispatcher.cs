// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Dispatches a SourceRefreshRequest for one refresh group to a least-loaded runner instance. Shared by the
/// recurring SourceRefreshJob and by SourceChangedService's targeted notification refreshes.
/// </summary>
public class SourceRefreshDispatcher
{
    private readonly ILogger<SourceRefreshDispatcher> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public SourceRefreshDispatcher(
        ILogger<SourceRefreshDispatcher> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    /// <summary>
    /// Selects a runner instance for the group and sends the refresh request. Returns false when no runner is
    /// available (the caller decides whether that means "retry next tick" or "drop").
    /// </summary>
    public virtual async Task<bool> DispatchRefresh(
        Guid organizationId,
        Guid runnerId,
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        List<string> watchedPaths,
        bool triggeredByNotification = false)
    {
        var runner = await _runnerSelection.SelectRunnerInstance(organizationId, runnerId);

        if (runner == null)
        {
            _logger.LogWarning("No available runners in pool {PoolId} for source {SourceUrl}@{SourceRevision}",
                runnerId, sourceUrl, sourceRevision);
            return false;
        }

        _logger.LogDebug("Selected runner {RunnerName} for source refresh: {SourceUrl}@{SourceRevision}",
            runner.InstanceName, sourceUrl, sourceRevision);

        // Dispatch to runner via SignalR (stateless - no tracking needed)
        await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
            RunnerEndpoints.SourceRefresh,
            new SourceRefreshRequest
            {
                OrganizationId = organizationId,
                SourceUrl = sourceUrl,
                SourceRevision = sourceRevision,
                SourceRevisionType = sourceRevisionType,
                SourceType = sourceType,
                WatchedPaths = watchedPaths,
                TriggeredByNotification = triggeredByNotification
            }
        );

        return true;
    }
}
