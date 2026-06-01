// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.ViewManagement;

/// <summary>
/// Service for managing database views that are applied after migrations.
/// Views are stored as SQL files in embedded resources and applied using idempotent syntax.
/// </summary>
public class ViewManager : IViewManager
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<ViewManager> _logger;
    private readonly IEnumerable<ViewAssemblySource> _additionalSources;

    public ViewManager(SnapCdDbContext dbContext, ILogger<ViewManager> logger, IEnumerable<ViewAssemblySource> additionalSources)
    {
        _dbContext = dbContext;
        _logger = logger;
        _additionalSources = additionalSources;
    }

    public async Task ApplyViewsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying SQL Server database views");

        var assemblySources = new List<(Assembly Assembly, string Prefix)>
        {
            (typeof(ViewManager).Assembly, "SnapCd.Server.Core.Views.SqlServer.")
        };

        foreach (var source in _additionalSources)
        {
            assemblySources.Add((source.Assembly, source.ResourcePrefix));
        }

        var totalApplied = 0;

        foreach (var (assembly, prefix) in assemblySources)
        {
            var viewResources = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(prefix) && name.EndsWith(".sql"))
                .OrderBy(name => name)
                .ToList();

            foreach (var resourceName in viewResources)
            {
                var viewName = resourceName
                    .Replace(prefix, "")
                    .Replace(".sql", "");

                try
                {
                    _logger.LogDebug("Applying view: {ViewName}", viewName);

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        _logger.LogWarning("Could not load resource: {ResourceName}", resourceName);
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    var sql = await reader.ReadToEndAsync(cancellationToken);

                    await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);

                    _logger.LogInformation("Successfully applied view: {ViewName}", viewName);
                    totalApplied++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply view: {ViewName}", viewName);
                    throw;
                }
            }
        }

        if (totalApplied == 0)
        {
            _logger.LogWarning("No view files found");
            return;
        }

        _logger.LogInformation("All {Count} views applied successfully", totalApplied);
    }
}
