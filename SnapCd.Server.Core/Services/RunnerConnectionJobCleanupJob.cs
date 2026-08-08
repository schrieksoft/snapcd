// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Hangfire recurring job that periodically cleans up old RunnerConnectionJob records.
/// Deletes records that are older than 10 minutes (based on ModifiedDateTime).
/// </summary>
public class RunnerConnectionJobCleanupJob
{
    private readonly ILogger<RunnerConnectionJobCleanupJob> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public RunnerConnectionJobCleanupJob(
        ILogger<RunnerConnectionJobCleanupJob> logger,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task ExecuteJob()
    {
        using var _ = SnapCd.Server.Core.Services.CallerContext.CallerContext.Begin(SnapCd.Server.Core.Services.CallerContext.CallerKind.System);
        _logger.LogDebug("Starting RunnerConnectionJob cleanup job");

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);

            // Find all RunnerConnectionJob records older than 10 minutes
            var oldRecords = await dbContext.RunnerConnectionJobs
                .Where(rcj => rcj.ModifiedDateTime < cutoffTime)
                .ToListAsync();

            if (oldRecords.Count == 0)
            {
                _logger.LogTrace("No old RunnerConnectionJob records found");
                return;
            }

            _logger.LogInformation(
                "Found {Count} RunnerConnectionJob record(s) older than 10 minutes to clean up",
                oldRecords.Count);

            // Delete the old records
            dbContext.RunnerConnectionJobs.RemoveRange(oldRecords);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully cleaned up {Count} old RunnerConnectionJob record(s)",
                oldRecords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RunnerConnectionJob cleanup");
        }

        _logger.LogDebug("RunnerConnectionJob cleanup job completed");
    }
}
