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
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Services.Logs;

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

/// <summary>Values every table's logs share, whatever family they belong to.</summary>
public static class JobLogs
{
    public const string DefaultTaskName = "Default";

    /// <summary>The sequence the pre-chunk <c>Logs</c> column reads as.</summary>
    public const long LegacySequence = 0;
}

public class JobLogReader<TJob, TChunk, TLogEntry>
    where TJob : class, IJobWithLogs
    where TChunk : JobLogChunk
    where TLogEntry : class, IJobLogEntry, new()
{


    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<JobLogReader<TJob, TChunk, TLogEntry>> _logger;

    public JobLogReader(SnapCdDbContext dbContext, ILogger<JobLogReader<TJob, TChunk, TLogEntry>> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// The parts of a job's log, in reading order, without their lines. <paramref name="moduleId"/> narrows
    /// them to one Module, which a transfer needs: two Modules run under one job, so their logs share a job id.
    /// </summary>
    public async Task<List<LogChunkSummary>> GetChunkSummaries(Guid jobId, Guid? moduleId = null, long afterSequence = -1)
    {

        var summaries = new List<LogChunkSummary>();

        if (afterSequence < JobLogs.LegacySequence)
        {
            var legacyBytes = await _dbContext.Set<TJob>()
                .Where(j => j.Id == jobId && j.Logs != null)
                .Select(j => (long?)EF.Functions.DataLength(j.Logs))
                .FirstOrDefaultAsync();

            if (legacyBytes is > 0)
                summaries.Add(new LogChunkSummary(JobLogs.LegacySequence, null, null, 0, legacyBytes.Value));
        }

        var chunks = (await Summaries(_dbContext.Set<TChunk>(), jobId, moduleId, afterSequence).ToListAsync())
            .OrderBy(c => c.Sequence);

        summaries.AddRange(chunks.Select(c =>
            new LogChunkSummary(c.Sequence, c.ModuleId, c.TaskNames.Split('\n'), c.LineCount, c.UncompressedBytes)));

        return summaries;
    }

    private static IQueryable<ChunkRow> Summaries(IQueryable<TChunk> chunks, Guid jobId, Guid? moduleId, long afterSequence) =>
        chunks
            .Where(c => c.JobId == jobId && c.Sequence > afterSequence && (moduleId == null || c.ModuleId == moduleId))
            .Select(c => new ChunkRow(c.Sequence, c.ModuleId, c.TaskNames, c.LineCount, c.UncompressedBytes));

    private record ChunkRow(long Sequence, Guid ModuleId, string TaskNames, int LineCount, int UncompressedBytes);

    /// <summary>The lines of the given parts of a job's log, ordered by when they were written.</summary>
    public async Task<List<TLogEntry>> ReadChunks(Guid jobId, IReadOnlyCollection<long> sequences, Guid? moduleId = null) =>
        InWrittenOrder(await ReadChunksUnordered(jobId, sequences, moduleId));

    public static List<TLogEntry> InWrittenOrder(IEnumerable<TLogEntry> entries) =>
        entries.OrderBy(e => e.Timestamp).ThenBy(e => e.BatchTimeStamp).ToList();

    private async Task<List<TLogEntry>> ReadChunksUnordered(Guid jobId, IReadOnlyCollection<long> sequences, Guid? moduleId)
    {

        var entries = new List<TLogEntry>();

        if (sequences.Contains(JobLogs.LegacySequence))
            entries.AddRange(await ReadLegacy(_dbContext, jobId, moduleId));

        var chunkSequences = sequences.Where(s => s != JobLogs.LegacySequence).ToList();
        if (chunkSequences.Count == 0)
            return entries;

        var chunks = (await Payloads(_dbContext.Set<TChunk>(), jobId, chunkSequences, moduleId).ToListAsync())
            .OrderBy(c => c.Sequence)
            .ToList();

        var contexts = await ModuleContexts(_dbContext, chunks.Select(c => c.ModuleId).Distinct().ToList());

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

            entries.AddRange(lines.Select(line => new TLogEntry
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

    private static IQueryable<ChunkPayload> Payloads(IQueryable<TChunk> chunks, Guid jobId, List<long> sequences, Guid? moduleId) =>
        chunks
            .Where(c => c.JobId == jobId && sequences.Contains(c.Sequence) && (moduleId == null || c.ModuleId == moduleId))
            .Select(c => new ChunkPayload(c.Sequence, c.ModuleId, c.BatchTimeStamp, c.Payload));

    private record ChunkPayload(long Sequence, Guid ModuleId, DateTimeOffset BatchTimeStamp, byte[] Payload);

    private record ModuleContext(string ModuleName, Guid NamespaceId, string NamespaceName, Guid StackId, string StackName);

    private static async Task<Dictionary<Guid, ModuleContext>> ModuleContexts(SnapCdDbContext _dbContext, List<Guid> moduleIds) =>
        await _dbContext.Modules
            .Where(m => moduleIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                Context = new ModuleContext(m.Name, m.NamespaceId, m.Namespace.Name, m.Namespace.StackId, m.Namespace.Stack.Name)
            })
            .ToDictionaryAsync(m => m.Id, m => m.Context);

    private async Task<List<TLogEntry>> ReadLegacy(SnapCdDbContext _dbContext, Guid jobId, Guid? moduleId)
    {
        var found = await _dbContext.Set<TJob>()
            .Where(j => j.Id == jobId)
            .Select(j => new { j.Logs })
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(found?.Logs))
            return [];

        List<TLogEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<TLogEntry>>(found.Logs) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not read the stored log of job {JobId}", jobId);
            return [Unreadable(jobId, moduleId ?? Guid.Empty, null)];
        }

        return moduleId is { } module ? entries.Where(e => e.ModuleId == module).ToList() : entries;
    }

    private static TLogEntry Unreadable(Guid jobId, Guid moduleId, ModuleContext? context) => new()
    {
        JobId = jobId,
        ModuleId = moduleId,
        ModuleName = context?.ModuleName,
        NamespaceName = context?.NamespaceName!,
        StackName = context?.StackName!,
        TaskName = JobLogs.DefaultTaskName,
        Level = LogEventLevel.Error,
        Message = "Part of this log could not be read.",
        Source = LogSource.Server
    };

    /// <summary>A job's whole log, ordered by when its lines were written.</summary>
    public async Task<List<TLogEntry>> GetLogEntries(Guid jobId, Guid? moduleId = null)
    {
        var summaries = await GetChunkSummaries(jobId, moduleId);
        return await ReadChunks(jobId, summaries.Select(s => s.Sequence).ToList(), moduleId);
    }

    /// <summary>One job's logs, by task, narrowed to one Module when <paramref name="moduleId"/> is given.</summary>
    public async Task<Dictionary<string, string>> GetLogStrings(Guid jobId, Guid? moduleId = null) =>
        GroupByTask(await GetLogEntries(jobId, moduleId));

    public static Dictionary<string, string> GroupByTask(IEnumerable<TLogEntry> entries) =>
        entries
            .GroupBy(LogKey)
            .ToDictionary(g => g.Key, g => string.Join(Environment.NewLine, g.Select(l => l.Message)));

    /// <summary>
    /// The task name, qualified by a log correlation where one is set. The discriminator says what
    /// the id is, so a reader can tell a wave from whatever is correlated on next.
    /// </summary>
    public static string LogKey(TLogEntry entry)
    {
        var taskName = entry.TaskName ?? JobLogs.DefaultTaskName;

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


        return await _dbContext.ProbeWaves
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
