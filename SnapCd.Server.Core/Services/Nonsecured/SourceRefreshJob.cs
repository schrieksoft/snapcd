// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Nonsecured.MaintenanceMode;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Services.Utils.CallerContext;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Nonsecured;

/// <summary>
/// Hangfire recurring job that periodically refreshes source revisions (resolves git references to commit SHAs).
/// Dispatches refresh requests to runners via SignalR.
/// </summary>
public class SourceRefreshJob
{
    private readonly ILogger<SourceRefreshJob> _logger;
    private readonly SourceRefreshDispatcher _dispatcher;
    private readonly SnapCdDbContext _dbContext;
    private readonly SourceRefreshSettings _settings;

    private readonly IMaintenanceModeService _maintenanceModeService;

    public SourceRefreshJob(
        ILogger<SourceRefreshJob> logger,
        SourceRefreshDispatcher dispatcher,
        SnapCdDbContext dbContext,
        IOptions<SourceRefreshSettings> settings,
        IMaintenanceModeService maintenanceModeService
    )
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _dbContext = dbContext;
        _settings = settings.Value;
        _maintenanceModeService = maintenanceModeService;
    }

    public async Task ExecuteJob()
    {
        using var _ = CallerContext.Begin(CallerKind.System);
        if (await _maintenanceModeService.IsActiveAsync())
        {
            _logger.LogInformation("Maintenance mode active: skipping source refresh cycle");
            return;
        }

        var modules = _dbContext.Modules
            .Include(x => x.Runner)
            .Include(x => x.AdditionalTriggerPaths)
            .Include(x => x.Resolved)
            .ToList();

        // A Module watches the paths declared on every Namespace above it, not only on its own.
        var chains = new Dictionary<Guid, List<Entities.Definition.Namespace>>();
        foreach (var organization in modules.GroupBy(m => m.OrganizationId))
        {
            var loaded = await InheritedNamespaces.ForNamespaces(
                _dbContext,
                organization.Select(m => m.NamespaceId).Distinct().ToList(),
                organization.Key,
                q => q.Include(n => n.AdditionalTriggerPaths));

            foreach (var (namespaceId, chain) in loaded) chains[namespaceId] = chain;
        }

        var groups = modules
            .GroupBy(x => new { x.SourceType, x.SourceUrl, x.SourceRevision, x.SourceRevisionType, x.OrganizationId, x.RunnerId })
            .Select(g => new
            {
                g.Key.SourceType,
                g.Key.SourceUrl,
                g.Key.SourceRevision,
                g.Key.SourceRevisionType,
                g.Key.OrganizationId,
                g.Key.RunnerId,
                // Empty keeps head-only semantics for the whole group.
                WatchedPaths = TriggerPathClosure.GroupWatchedPaths(g, chains)
            })
            .ToList();

        _logger.LogInformation("Starting source refresh job for {Count} unique sources", groups.Count);

        foreach (var group in groups)
            try
            {
                await _dispatcher.DispatchRefresh(
                    group.OrganizationId,
                    group.RunnerId,
                    group.SourceUrl,
                    group.SourceRevision,
                    group.SourceType,
                    group.SourceRevisionType,
                    group.WatchedPaths);
            }
            catch (RunnerUnreachableException ex)
            {
                // Expected while a runner is reconnecting or a dead server's connections are being
                // swept: the next run of this job picks the source up again.
                _logger.LogWarning("Skipping source refresh for {SourceUrl}@{SourceRevision}: {Reason}",
                    group.SourceUrl, group.SourceRevision, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching source refresh for {SourceUrl}@{SourceRevision}",
                    group.SourceUrl, group.SourceRevision);
                // Continue with next source - don't fail entire batch
            }

        _logger.LogInformation("Source refresh job completed");
    }
}
