using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services;

public class LogService
{
    private readonly SnapCdDbContext _dbContext;

    public LogService(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddLogEntries(List<LogEntryDto> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        // Group log entries by correlationId (ModuleJob Id)
        var groupedLogs = logEntries.GroupBy(l => l.JobId);

        foreach (var group in groupedLogs)
        {
            var correlationId = group.Key;

            // Use a transaction with Serializable isolation to ensure proper locking
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // Set lock timeout to 90 seconds (90000 milliseconds)
                await _dbContext.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 180000");

                // Lock the row using UPDLOCK and ROWLOCK hints
                var moduleJob = await _dbContext.ModuleJobs
                    .FromSqlRaw("SELECT * FROM ModuleJobs WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", correlationId)
                    .FirstOrDefaultAsync();

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

                await _dbContext.SaveChangesAsync();
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
        var moduleJob = await _dbContext.ModuleJobs
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