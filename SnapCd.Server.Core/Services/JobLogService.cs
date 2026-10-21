// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Appends and reads one table's runner logs. Closed over the job entity and the log type that
/// table stores, so a caller names the table it means and nothing has to work out which of several
/// tables an id belongs to. Written once and closed three ways, as OutputParamResolver is.
/// </summary>
public class JobLogService<TJob, TLogEntry>
    where TJob : class, IJobWithLogs
    where TLogEntry : class, IJobLogEntry
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public JobLogService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task AddLogEntries(List<TLogEntry> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var groupedLogs = logEntries.GroupBy(l => l.JobId);

        foreach (var group in groupedLogs)
        {
            var correlationId = group.Key;

            // ReadCommitted + the UPDLOCK hint below is what serializes concurrent appenders to the
            // same job: the second writer blocks on the row's U lock and reads the committed Logs
            // after the first commits. Serializable adds key-range locking on top of that (the
            // clustered PK is (Id, OrganizationId), so "WHERE Id = @0" is a prefix-range seek) and
            // is not needed for this read-modify-write of a single existing row.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 180000");

                // The U lock has to come from a table hint, and EF has no API for one, so this
                // is the one place raw SQL is unavoidable. The table name is EF's own, taken from
                // the model rather than written out, so renaming a table cannot leave it stale.
                var table = dbContext.Model.FindEntityType(typeof(TJob))!.GetTableName();

                // Single, not First: Id is the primary key, so at most one row can match, and EF
                // cannot see the filter inside raw SQL.
                var job = await dbContext.Set<TJob>()
                    .FromSqlRaw($"SELECT * FROM [{table}] WITH (UPDLOCK, ROWLOCK) WHERE Id = {{0}}", correlationId)
                    .SingleOrDefaultAsync();

                if (job == null)
                {
                    // A runner-side failure reaches the operator only through its logs.
                    _logger.LogWarning(
                        "Discarding {Count} log entries for job {JobId}: no {JobType} with that id",
                        group.Count(), correlationId, typeof(TJob).Name);

                    await transaction.RollbackAsync();
                    continue;
                }

                var existingLogs = Deserialize(job.Logs);
                existingLogs.AddRange(group);
                existingLogs = existingLogs.OrderBy(l => l.Timestamp).ThenBy(l => l.BatchTimeStamp).ToList();

                job.Logs = JsonSerializer.Serialize(existingLogs);

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<List<TLogEntry>> GetLogEntries(Guid correlationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // A row with no logs yet is not a missing row, so the holder rather than the value is what
        // the lookup tests.
        var found = await dbContext.Set<TJob>()
            .Where(j => j.Id == correlationId)
            .Select(j => new { j.Logs })
            .FirstOrDefaultAsync();

        return Deserialize(found?.Logs);
    }

    public async Task<string> GetLogString(Guid correlationId)
    {
        var logEntries = await GetLogEntries(correlationId);

        return logEntries.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, logEntries.Select(l => l.Message));
    }

    /// <summary>
    /// One job's logs, by task. <paramref name="moduleId"/> narrows them to one Module, which a
    /// transfer needs: two Modules run under one job, so their logs share a job id.
    ///
    /// A task that runs more than once in a job sets a log correlation, and each of its runs is
    /// keyed separately: a probe is dispatched once per wave the Module owes, under the one task
    /// name, so grouping on the name alone would merge answers to different questions.
    /// </summary>
    public async Task<Dictionary<string, string>> GetLogStrings(Guid correlationId, Guid? moduleId = null)
    {
        var logEntries = await GetLogEntries(correlationId);

        if (moduleId is { } module)
            logEntries = logEntries.Where(l => l.ModuleId == module).ToList();

        return logEntries
            .GroupBy(LogKey)
            .ToDictionary(
                group => group.Key,
                group => string.Join(Environment.NewLine, group.Select(l => l.Message)));
    }

    /// <summary>
    /// The task name, qualified by a log correlation where one is set. The discriminator says
    /// what the id is, so a reader can tell a wave from whatever is correlated on next.
    /// </summary>
    private static string LogKey(TLogEntry entry)
    {
        var taskName = entry.TaskName ?? "Default";

        return entry is { LogCorrelationId: { } id, LogCorrelationDiscriminator: { } kind }
            ? $"{taskName} ({kind} {id})"
            : taskName;
    }

    /// <summary>Malformed stored logs start fresh rather than failing the append.</summary>
    private static List<TLogEntry> Deserialize(string? logs)
    {
        if (string.IsNullOrEmpty(logs)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<TLogEntry>>(logs) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
