// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Logging;

/// <summary>
/// Default <see cref="IJobLogStream"/>, for one table that stores runner logs. Each job gets its own
/// <see cref="JobLogQueue{TLogEntry}"/>, so one job's log cannot hold back another's. Buffering through a
/// disconnect is <see cref="RunnerHubConnection.SendLogsAsync{TLogEntry}"/>'s.
/// </summary>
public sealed class HubJobLogStream<TLogEntry> : IJobLogStream, IAsyncDisposable
    where TLogEntry : class, IJobLogEntry, new()
{
    private readonly ConcurrentDictionary<Guid, JobLogQueue<TLogEntry>> _queues = new();
    private readonly object _gate = new();
    private readonly RunnerHubConnection _hub;
    private readonly JobLogStreamSettings _settings;
    private readonly ILogger _logger;

    private readonly string _endpoint;

    public HubJobLogStream(
        RunnerHubConnection hub,
        string endpoint,
        JobLogStreamSettings settings,
        ILogger logger)
    {
        _hub = hub;
        _endpoint = endpoint;
        _settings = settings;
        _logger = logger;
    }

    public Task EmitAsync(JobLogEnvelope envelope, string message, CancellationToken ct = default)
    {
        var line = new TLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            JobId = envelope.JobId,
            ModuleId = envelope.ModuleId,
            StackName = envelope.StackName,
            NamespaceName = envelope.NamespaceName,
            ModuleName = envelope.ModuleName,
            TaskName = envelope.TaskName,
            Level = MapLevel(envelope.Level),
            Message = message,
            Source = LogSource.Runner,
            LogCorrelationId = envelope.LogCorrelationId,
            LogCorrelationDiscriminator = envelope.LogCorrelationDiscriminator
        };

        lock (_gate)
        {
            while (!QueueFor(envelope.JobId).TryEnqueue(line))
                _queues.TryRemove(envelope.JobId, out _);
        }

        return Task.CompletedTask;
    }

    public async Task DrainAsync(Guid jobId)
    {
        if (!_queues.TryGetValue(jobId, out var queue))
            return;

        if (!await queue.DrainAsync(TimeSpan.FromSeconds(_settings.DrainTimeoutSeconds)))
            _logger.LogWarning(
                "Job {JobId} still had log lines waiting to ship after {Seconds} seconds; reporting its step anyway",
                jobId, _settings.DrainTimeoutSeconds);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_queues.Keys.Select(DrainAsync));
    }

    private JobLogQueue<TLogEntry> QueueFor(Guid jobId)
    {
        if (_queues.TryGetValue(jobId, out var queue) && !queue.IsCompleted)
            return queue;

        queue = new JobLogQueue<TLogEntry>(
            jobId, batch => _hub.SendLogsAsync(_endpoint, batch), _settings, _logger);
        _queues[jobId] = queue;
        return queue;
    }

    private static LogEventLevel MapLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information,
    };
}
