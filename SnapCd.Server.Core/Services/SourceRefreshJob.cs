// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Hangfire recurring job that periodically refreshes source revisions (resolves git references to commit SHAs).
/// Dispatches refresh requests to runners via SignalR.
/// </summary>
public class SourceRefreshJob
{
    private readonly ILogger<SourceRefreshJob> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly SnapCdDbContext _dbContext;
    private readonly SourceRefreshSettings _settings;

    public SourceRefreshJob(
        ILogger<SourceRefreshJob> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        SnapCdDbContext dbContext,
        IOptions<SourceRefreshSettings> settings
    )
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
        _dbContext = dbContext;
        _settings = settings.Value;
    }

    public async Task ExecuteJob()
    {
        var modules = _dbContext.Modules
            .Include(x => x.Runner)
            .GroupBy(x => new { x.SourceType, x.SourceUrl, x.SourceRevision, x.SourceRevisionType, x.OrganizationId, x.RunnerId })
            .Select(g => new
            {
                g.Key.SourceType,
                g.Key.SourceUrl,
                g.Key.SourceRevision,
                g.Key.SourceRevisionType,
                g.Key.OrganizationId,
                g.Key.RunnerId
            })
            .ToList();

        _logger.LogInformation("Starting source refresh job for {Count} unique sources", modules.Count);

        foreach (var module in modules)
            try
            {
                // Select runner using least-loaded strategy
                var runner = await _runnerSelection.SelectRunnerInstance(module.OrganizationId, module.RunnerId);

                if (runner == null)
                {
                    _logger.LogWarning("No available runners in pool {PoolId} for source {SourceUrl}@{SourceRevision}",
                        module.RunnerId, module.SourceUrl, module.SourceRevision);
                    continue; // Skip this source, try again on next job run
                }

                _logger.LogDebug("Selected runner {RunnerName} for source refresh: {SourceUrl}@{SourceRevision}",
                    runner.InstanceName, module.SourceUrl, module.SourceRevision);

                // Dispatch to runner via SignalR (stateless - no tracking needed)
                await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                    RunnerEndpoints.SourceRefresh,
                    new SourceRefreshRequest
                    {
                        OrganizationId = module.OrganizationId,
                        SourceUrl = module.SourceUrl,
                        SourceRevision = module.SourceRevision,
                        SourceRevisionType = module.SourceRevisionType,
                        SourceType = module.SourceType
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching source refresh for {SourceUrl}@{SourceRevision}",
                    module.SourceUrl, module.SourceRevision);
                // Continue with next source - don't fail entire batch
            }

        _logger.LogInformation("Source refresh job completed");
    }
}