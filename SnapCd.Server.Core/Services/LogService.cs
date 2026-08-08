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
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services;

public class LogService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public LogService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddLogEntries(List<LogEntryDto> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Group log entries by correlationId (ModuleJob Id)
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
                // Set lock timeout to 90 seconds (90000 milliseconds)
                await dbContext.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 180000");

                // Lock the row using UPDLOCK and ROWLOCK hints. Single, not First: Id is the primary
                // key, so at most one row can match, and EF cannot see the filter inside raw SQL.
                var moduleJob = await dbContext.ModuleJobs
                    .FromSqlRaw("SELECT * FROM ModuleJobs WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", correlationId)
                    .SingleOrDefaultAsync();

                if (moduleJob == null)
                    continue;

                // Parse existing logs or create new array
                List<LogEntryDto> existingLogs = new();
                if (!string.IsNullOrEmpty(moduleJob.Logs))
                    try
                    {
                        existingLogs = JsonSerializer.Deserialize<List<LogEntryDto>>(moduleJob.Logs) ?? new List<LogEntryDto>();
                    }
                    catch
                    {
                        // If deserialization fails, start fresh
                        existingLogs = new List<LogEntryDto>();
                    }

                // Append new log entries
                existingLogs.AddRange(group);

                // Sort by timestamp to maintain proper ordering
                existingLogs = existingLogs.OrderBy(l => l.Timestamp).ThenBy(l => l.BatchTimeStamp).ToList();

                // Serialize back to JSON
                moduleJob.Logs = JsonSerializer.Serialize(existingLogs);

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

    public async Task<List<LogEntryDto>> GetLogEntries(Guid correlationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var moduleJob = await dbContext.ModuleJobs
            .Where(j => j.Id == correlationId)
            .Select(j => j.Logs)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(moduleJob))
            return new List<LogEntryDto>();

        try
        {
            return JsonSerializer.Deserialize<List<LogEntryDto>>(moduleJob) ?? new List<LogEntryDto>();
        }
        catch
        {
            return new List<LogEntryDto>();
        }
    }

    public async Task<string> GetLogString(Guid correlationId)
    {
        var logEntries = await GetLogEntries(correlationId);

        if (logEntries.Count == 0)
            return string.Empty;

        return string.Join(Environment.NewLine, logEntries.Select(l => l.Message));
    }

    public async Task<Dictionary<string, string>> GetLogStrings(Guid correlationId)
    {
        var logEntries = await GetLogEntries(correlationId);

        var result = new Dictionary<string, string>();

        // Group by LogContext (e.g., "Init", "Plan", "Apply", etc.)
        var groupedByContext = logEntries.GroupBy(l => l.TaskName ?? "Default");

        foreach (var group in groupedByContext)
        {
            var contextName = group.Key;
            var contextLogs = string.Join(Environment.NewLine, group.Select(l => l.Message));
            result[contextName] = contextLogs;
        }

        return result;
    }
}