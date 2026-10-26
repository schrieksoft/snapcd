// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Logs;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// A summary of one stored part of a job's log, read without decompressing it. Sequence 0 is the log a job
/// stored before logs were chunked, whose tasks are not known without reading it.
/// </summary>
public record LogChunkSummary(long Sequence, Guid? ModuleId, IReadOnlyList<string>? TaskNames, int LineCount, long UncompressedBytes)
{
    public bool HasTask(string taskName) => TaskNames is null || TaskNames.Contains(taskName);
}

/// <summary>Where a probe wave started, for naming it to a reader.</summary>
public record ProbeWaveOrigin(Guid WaveId, string StackName, string NamespaceName, string ModuleName);

public class LogService
{
    public const string DefaultTaskName = "Default";

    /// <summary>The sequence the pre-chunk <c>Logs</c> column reads as.</summary>
    public const long LegacySequence = 0;

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<LogService> _logger;

    public LogService(IDbContextFactory<SnapCdDbContext> dbContextFactory, ILogger<LogService> logger)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddLogEntries(List<LogEntryDto> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (var job in logEntries.GroupBy(l => l.JobId))
        {
            var moduleJobOrganization = await dbContext.ModuleJobs
                .Where(j => j.Id == job.Key)
                .Select(j => (Guid?)j.OrganizationId)
                .SingleOrDefaultAsync();

            var stateMigrationOrganization = moduleJobOrganization is null
                ? await dbContext.StateMigrationJobs
                    .Where(j => j.Id == job.Key)
                    .Select(j => (Guid?)j.OrganizationId)
                    .SingleOrDefaultAsync()
                : null;

            if (moduleJobOrganization is null && stateMigrationOrganization is null)
            {
                // A runner-side failure reaches the operator only through its logs.
                _logger.LogWarning(
                    "Discarding {Count} log entries for job {JobId}: no job with that id exists",
                    job.Count(), job.Key);
                continue;
            }

            var stamps = job.Select(l => l.BatchTimeStamp).Distinct().ToList();
            var stored = moduleJobOrganization is not null
                ? await StoredBatches(dbContext.ModuleJobLogChunks, job.Key, stamps)
                : await StoredBatches(dbContext.StateMigrationJobLogChunks, job.Key, stamps);

            foreach (var batch in job.GroupBy(l => (l.ModuleId, l.BatchTimeStamp)))
            {
                if (stored.Contains(batch.Key))
                {
                    _logger.LogDebug("Ignoring a resent log batch for job {JobId}", job.Key);
                    continue;
                }

                JobLogChunk chunk = moduleJobOrganization is { } organizationId
                    ? new ModuleJobLogChunk { OrganizationId = organizationId }
                    : new StateMigrationJobLogChunk { OrganizationId = stateMigrationOrganization!.Value };

                chunk.Id = Guid.NewGuid();
                chunk.JobId = job.Key;
                chunk.ModuleId = batch.Key.ModuleId;
                LogChunkCodec.Encode(chunk, batch.OrderBy(l => l.Timestamp).ToList());

                dbContext.Add(chunk);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<HashSet<(Guid ModuleId, DateTimeOffset BatchTimeStamp)>> StoredBatches<T>(
        DbSet<T> chunks, Guid jobId, List<DateTimeOffset> stamps) where T : JobLogChunk =>
        (await chunks
            .Where(c => c.JobId == jobId && stamps.Contains(c.BatchTimeStamp))
            .Select(c => new { c.ModuleId, c.BatchTimeStamp })
            .ToListAsync())
        .Select(c => (c.ModuleId, c.BatchTimeStamp))
        .ToHashSet();

    /// <summary>
    /// The parts of a job's log, in reading order, without their lines. <paramref name="moduleId"/> narrows
    /// them to one Module, which a transfer needs: two Modules run under one job, so their logs share a job id.
    /// </summary>
    public async Task<List<LogChunkSummary>> GetChunkSummaries(Guid jobId, Guid? moduleId = null, long afterSequence = -1)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var summaries = new List<LogChunkSummary>();

        if (afterSequence < LegacySequence)
        {
            var legacyBytes = await dbContext.ModuleJobs
                    .Where(j => j.Id == jobId && j.Logs != null)
                    .Select(j => (long?)EF.Functions.DataLength(j.Logs))
                    .FirstOrDefaultAsync()
                ?? await dbContext.StateMigrationJobs
                    .Where(j => j.Id == jobId && j.Logs != null)
                    .Select(j => (long?)EF.Functions.DataLength(j.Logs))
                    .FirstOrDefaultAsync();

            if (legacyBytes is > 0)
                summaries.Add(new LogChunkSummary(LegacySequence, null, null, 0, legacyBytes.Value));
        }

        var chunks = (await Summaries(dbContext.ModuleJobLogChunks, jobId, moduleId, afterSequence).ToListAsync())
            .Concat(await Summaries(dbContext.StateMigrationJobLogChunks, jobId, moduleId, afterSequence).ToListAsync())
            .OrderBy(c => c.Sequence);

        summaries.AddRange(chunks.Select(c =>
            new LogChunkSummary(c.Sequence, c.ModuleId, c.TaskNames.Split('\n'), c.LineCount, c.UncompressedBytes)));

        return summaries;
    }

    private static IQueryable<ChunkRow> Summaries<T>(DbSet<T> chunks, Guid jobId, Guid? moduleId, long afterSequence)
        where T : JobLogChunk =>
        chunks
            .Where(c => c.JobId == jobId && c.Sequence > afterSequence && (moduleId == null || c.ModuleId == moduleId))
            .Select(c => new ChunkRow(c.Sequence, c.ModuleId, c.TaskNames, c.LineCount, c.UncompressedBytes));

    private record ChunkRow(long Sequence, Guid ModuleId, string TaskNames, int LineCount, int UncompressedBytes);

    /// <summary>The lines of the given parts of a job's log, ordered by when they were written.</summary>
    public async Task<List<LogEntryDto>> ReadChunks(Guid jobId, IReadOnlyCollection<long> sequences, Guid? moduleId = null) =>
        InWrittenOrder(await ReadChunksUnordered(jobId, sequences, moduleId));

    public static List<LogEntryDto> InWrittenOrder(IEnumerable<LogEntryDto> entries) =>
        entries.OrderBy(e => e.Timestamp).ThenBy(e => e.BatchTimeStamp).ToList();

    private async Task<List<LogEntryDto>> ReadChunksUnordered(Guid jobId, IReadOnlyCollection<long> sequences, Guid? moduleId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var entries = new List<LogEntryDto>();

        if (sequences.Contains(LegacySequence))
            entries.AddRange(await ReadLegacy(dbContext, jobId, moduleId));

        var chunkSequences = sequences.Where(s => s != LegacySequence).ToList();
        if (chunkSequences.Count == 0)
            return entries;

        var chunks = (await Payloads(dbContext.ModuleJobLogChunks, jobId, chunkSequences, moduleId).ToListAsync())
            .Concat(await Payloads(dbContext.StateMigrationJobLogChunks, jobId, chunkSequences, moduleId).ToListAsync())
            .OrderBy(c => c.Sequence)
            .ToList();

        var contexts = await ModuleContexts(dbContext, chunks.Select(c => c.ModuleId).Distinct().ToList());

        foreach (var chunk in chunks)
        {
            var context = contexts.GetValueOrDefault(chunk.ModuleId);

            List<StoredLogLine> lines;
            try
            {
                lines = LogChunkCodec.Decode(chunk.Payload);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                _logger.LogWarning(ex, "Could not read log chunk {Sequence} of job {JobId}", chunk.Sequence, jobId);
                entries.Add(Unreadable(jobId, chunk.ModuleId, context));
                continue;
            }

            entries.AddRange(lines.Select(line => new LogEntryDto
            {
                JobId = jobId,
                ModuleId = chunk.ModuleId,
                ModuleName = context?.ModuleName,
                NamespaceId = context?.NamespaceId,
                NamespaceName = context?.NamespaceName!,
                StackId = context?.StackId,
                StackName = context?.StackName!,
                Timestamp = line.Timestamp,
                BatchTimeStamp = chunk.BatchTimeStamp,
                TaskName = line.TaskName!,
                Level = line.Level,
                Message = line.Message!,
                Tags = line.Tags,
                Source = line.Source,
                LogCorrelationId = line.LogCorrelationId,
                LogCorrelationDiscriminator = line.LogCorrelationDiscriminator
            }));
        }

        return entries;
    }

    private static IQueryable<ChunkPayload> Payloads<T>(DbSet<T> chunks, Guid jobId, List<long> sequences, Guid? moduleId)
        where T : JobLogChunk =>
        chunks
            .Where(c => c.JobId == jobId && sequences.Contains(c.Sequence) && (moduleId == null || c.ModuleId == moduleId))
            .Select(c => new ChunkPayload(c.Sequence, c.ModuleId, c.BatchTimeStamp, c.Payload));

    private record ChunkPayload(long Sequence, Guid ModuleId, DateTimeOffset BatchTimeStamp, byte[] Payload);

    private record ModuleContext(string ModuleName, Guid NamespaceId, string NamespaceName, Guid StackId, string StackName);

    private static async Task<Dictionary<Guid, ModuleContext>> ModuleContexts(SnapCdDbContext dbContext, List<Guid> moduleIds) =>
        await dbContext.Modules
            .Where(m => moduleIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                Context = new ModuleContext(m.Name, m.NamespaceId, m.Namespace.Name, m.Namespace.StackId, m.Namespace.Stack.Name)
            })
            .ToDictionaryAsync(m => m.Id, m => m.Context);

    private async Task<List<LogEntryDto>> ReadLegacy(SnapCdDbContext dbContext, Guid jobId, Guid? moduleId)
    {
        // A job id belongs to one family or the other. A row with no logs yet is
        // not a missing row, so the holder distinguishes them.
        var found = await dbContext.ModuleJobs
                        .Where(j => j.Id == jobId)
                        .Select(j => new { j.Logs })
                        .FirstOrDefaultAsync()
                    ?? await dbContext.StateMigrationJobs
                        .Where(j => j.Id == jobId)
                        .Select(j => new { j.Logs })
                        .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(found?.Logs))
            return [];

        List<LogEntryDto> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<LogEntryDto>>(found.Logs) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not read the stored log of job {JobId}", jobId);
            return [Unreadable(jobId, moduleId ?? Guid.Empty, null)];
        }

        return moduleId is { } module ? entries.Where(e => e.ModuleId == module).ToList() : entries;
    }

    private static LogEntryDto Unreadable(Guid jobId, Guid moduleId, ModuleContext? context) => new()
    {
        JobId = jobId,
        ModuleId = moduleId,
        ModuleName = context?.ModuleName,
        NamespaceName = context?.NamespaceName!,
        StackName = context?.StackName!,
        TaskName = DefaultTaskName,
        Level = LogEventLevel.Error,
        Message = "Part of this log could not be read.",
        Source = LogSource.Server
    };

    /// <summary>A job's whole log, ordered by when its lines were written.</summary>
    public async Task<List<LogEntryDto>> GetLogEntries(Guid jobId, Guid? moduleId = null)
    {
        var summaries = await GetChunkSummaries(jobId, moduleId);
        return await ReadChunks(jobId, summaries.Select(s => s.Sequence).ToList(), moduleId);
    }

    public async Task<string> GetLogString(Guid jobId)
    {
        var logEntries = await GetLogEntries(jobId);

        if (logEntries.Count == 0)
            return string.Empty;

        return string.Join(Environment.NewLine, logEntries.Select(l => l.Message));
    }

    /// <summary>One job's logs, by task, narrowed to one Module when <paramref name="moduleId"/> is given.</summary>
    public async Task<Dictionary<string, string>> GetLogStrings(Guid jobId, Guid? moduleId = null) =>
        GroupByTask(await GetLogEntries(jobId, moduleId));

    public static Dictionary<string, string> GroupByTask(IEnumerable<LogEntryDto> entries) =>
        entries
            .GroupBy(LogKey)
            .ToDictionary(g => g.Key, g => string.Join(Environment.NewLine, g.Select(l => l.Message)));

    /// <summary>
    /// The task name, qualified by a log correlation where one is set. The discriminator says what
    /// the id is, so a reader can tell a wave from whatever is correlated on next.
    /// </summary>
    public static string LogKey(LogEntryDto entry)
    {
        var taskName = entry.TaskName ?? DefaultTaskName;

        return entry is { LogCorrelationId: { } id, LogCorrelationDiscriminator: { } kind }
            ? $"{taskName} ({kind} {id})"
            : taskName;
    }

    /// <summary>
    /// Where each of these probe waves came from, as namespace and Module. A wave's id says nothing
    /// to a reader; the Module whose change started it is the thing they recognise.
    /// </summary>
    public async Task<Dictionary<Guid, ProbeWaveOrigin>> ProbeWaveOrigins(
        IReadOnlyCollection<Guid> waveIds,
        Guid organizationId)
    {
        if (waveIds.Count == 0) return [];

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => waveIds.Contains(w.Id) && w.OrganizationId == organizationId)
            .Select(w => new ProbeWaveOrigin(
                w.Id,
                w.OriginModuleJob.Module.Namespace.Stack.Name,
                w.OriginModuleJob.Module.Namespace.Name,
                w.OriginModuleJob.Module.Name))
            .ToDictionaryAsync(o => o.WaveId);
    }
}
