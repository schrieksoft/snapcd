// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Services.Logs;

public class JobLogWriterFactory(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ILoggerFactory loggerFactory)
{
    public JobLogWriter<TJob, TChunk, TLogEntry> Create<TJob, TChunk, TLogEntry>()
        where TJob : class, IJobWithLogs
        where TChunk : JobLogChunk, new()
        where TLogEntry : class, IJobLogEntry =>
        new(dbContextFactory, loggerFactory.CreateLogger<JobLogWriter<TJob, TChunk, TLogEntry>>());
}

/// <summary>
/// Stores one table's runner logs, closed over that table's chunk type and the log type it
/// carries. The caller names the table, so a job id is never looked up across tables.
///
/// A runner authenticates as itself rather than as a person, so nothing here is gated on a
/// principal; the hub checks the caller is the runner the job was given to.
/// </summary>
public class JobLogWriter<TJob, TChunk, TLogEntry>(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ILogger<JobLogWriter<TJob, TChunk, TLogEntry>> logger)
    where TJob : class, IJobWithLogs
    where TChunk : JobLogChunk, new()
    where TLogEntry : class, IJobLogEntry
{
    public async Task AddLogEntries(List<TLogEntry> logEntries, Guid organizationId)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        foreach (var job in logEntries.GroupBy(l => l.JobId))
        {
            // One unknown job id in a batch must not cost the other jobs their lines, and the
            // chunk's foreign key refuses a row whose job is gone.
            if (!await dbContext.Set<TJob>().AnyAsync(j => j.Id == job.Key && j.OrganizationId == organizationId))
            {
                logger.LogWarning(
                    "Discarding {Count} log entries for job {JobId}: no job with that id exists",
                    job.Count(), job.Key);
                continue;
            }

            var stamps = job.Select(l => l.BatchTimeStamp).Distinct().ToList();

            var stored = (await dbContext.Set<TChunk>()
                    .Where(c => c.JobId == job.Key && stamps.Contains(c.BatchTimeStamp))
                    .Select(c => new { c.ModuleId, c.BatchTimeStamp })
                    .ToListAsync())
                .Select(c => (c.ModuleId, c.BatchTimeStamp))
                .ToHashSet();

            foreach (var batch in job.GroupBy(l => (l.ModuleId, l.BatchTimeStamp)))
            {
                if (stored.Contains(batch.Key))
                {
                    logger.LogDebug("Ignoring a resent log batch for job {JobId}", job.Key);
                    continue;
                }

                var chunk = new TChunk
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    JobId = job.Key,
                    ModuleId = batch.Key.ModuleId
                };

                LogChunkCodec.Encode(chunk, batch.OrderBy(l => l.Timestamp).Cast<IJobLogEntry>().ToList());

                dbContext.Add(chunk);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
