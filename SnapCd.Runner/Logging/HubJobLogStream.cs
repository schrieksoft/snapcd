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
/// Default <see cref="IJobLogStream"/>. Each job gets its own <see cref="JobLogQueue"/>, so one job's log
/// cannot hold back another's. Buffering through a disconnect is <see cref="RunnerHubConnection.SendLogsAsync"/>'s.
/// </summary>
public sealed class HubJobLogStream : IJobLogStream, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, JobLogQueue> _queues = new();
    private readonly object _gate = new();
    private readonly RunnerHubConnection _hub;
    private readonly JobLogStreamSettings _settings;
    private readonly ILogger<HubJobLogStream> _logger;

    public HubJobLogStream(RunnerHubConnection hub, IOptions<JobLogStreamSettings> settings, ILogger<HubJobLogStream> logger)
    {
        _hub = hub;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task EmitAsync(JobLogEnvelope envelope, string message, CancellationToken ct = default)
    {
        var line = new LogEntryDto
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

    private JobLogQueue QueueFor(Guid jobId)
    {
        if (_queues.TryGetValue(jobId, out var queue) && !queue.IsCompleted)
            return queue;

        queue = new JobLogQueue(jobId, _hub.SendLogsAsync, _settings, _logger);
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
