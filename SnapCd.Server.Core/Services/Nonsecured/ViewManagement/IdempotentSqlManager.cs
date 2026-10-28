// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.ViewManagement;

public class IdempotentSqlManager : IIdempotentSqlManager
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<IdempotentSqlManager> _logger;
    private readonly IEnumerable<IdempotentSqlAssemblySource> _additionalSources;

    public IdempotentSqlManager(SnapCdDbContext dbContext, ILogger<IdempotentSqlManager> logger, IEnumerable<IdempotentSqlAssemblySource> additionalSources)
    {
        _dbContext = dbContext;
        _logger = logger;
        _additionalSources = additionalSources;
    }

    public async Task ApplyIdempotentSqlAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Applying idempotent SQL scripts");

        var assemblySources = new List<(Assembly Assembly, string Prefix)>
        {
            (typeof(IdempotentSqlManager).Assembly, "SnapCd.Server.Core.Views.SqlServer.")
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
                var scriptName = resourceName
                    .Replace(prefix, "")
                    .Replace(".sql", "");

                try
                {
                    _logger.LogDebug("Applying script: {ScriptName}", scriptName);

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        _logger.LogWarning("Could not load resource: {ResourceName}", resourceName);
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    var sql = await reader.ReadToEndAsync(cancellationToken);

                    var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                        .Where(b => !string.IsNullOrWhiteSpace(b));

                    foreach (var batch in batches)
                    {
                        await _dbContext.Database.ExecuteSqlRawAsync(batch, cancellationToken);
                    }

                    _logger.LogDebug("Successfully applied script: {ScriptName}", scriptName);
                    totalApplied++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply script: {ScriptName}", scriptName);
                    throw;
                }
            }
        }

        if (totalApplied == 0)
        {
            _logger.LogWarning("No SQL script files found");
            return;
        }

        _logger.LogInformation("All {Count} scripts applied successfully", totalApplied);
    }
}
