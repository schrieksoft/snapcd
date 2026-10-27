// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using Serilog.Events;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Logging;

/// <summary>
/// One job's log lines waiting to ship. Each job has its own queue and sender, so a job writing a large log
/// delays only itself. Lines leave in the order they were written.
/// </summary>
public sealed class JobLogQueue<TLogEntry>
    where TLogEntry : class, IJobLogEntry, new()
{
    private readonly Queue<TLogEntry> _lines = new();
    private readonly object _gate = new();
    private readonly Func<List<TLogEntry>, Task> _send;
    private readonly JobLogStreamSettings _settings;
    private readonly ILogger _logger;
    private readonly Guid _jobId;
    private readonly Task _sender;

    private TaskCompletionSource _wake = NewSignal();
    private TaskCompletionSource _sendNow = NewSignal();
    private long _pendingBytes;
    private int _dropped;
    private TLogEntry? _lastLine;
    private bool _warned;
    private bool _sending;
    private bool _completed;
    private bool _sentFirst;

    public JobLogQueue(Guid jobId, Func<List<TLogEntry>, Task> send, JobLogStreamSettings settings, ILogger logger)
    {
        _jobId = jobId;
        _send = send;
        _settings = settings;
        _logger = logger;
        _sender = Task.Run(SendLoopAsync);
    }

    /// <summary>True once the queue has been idle long enough to be retired, after which it accepts nothing.</summary>
    public bool IsCompleted
    {
        get
        {
            lock (_gate)
                return _completed;
        }
    }

    /// <summary>Queues a line. Returns false if the queue has been retired, so the caller makes a new one.</summary>
    public bool TryEnqueue(TLogEntry line)
    {
        lock (_gate)
        {
            if (_completed)
                return false;

            _lastLine = line;

            if (_lines.Count >= _settings.QueueLimit)
            {
                _dropped++;
                return true;
            }

            _lines.Enqueue(line);
            _pendingBytes += line.Message?.Length ?? 0;

            if (!_warned && _lines.Count >= _settings.QueueWarningThreshold)
            {
                _warned = true;
                _logger.LogWarning("Job {JobId} has {Count} log lines waiting to ship", _jobId, _lines.Count);
            }

            if (_lines.Count >= _settings.BatchSizeLimit || _pendingBytes >= _settings.BatchBytesLimit)
                _sendNow.TrySetResult();

            _wake.TrySetResult();
            return true;
        }
    }

    /// <summary>Waits until every queued line has been handed to the connection, or the timeout passes.</summary>
    public async Task<bool> DrainAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            lock (_gate)
            {
                if (_lines.Count == 0 && _dropped == 0 && !_sending)
                    return true;
                _sendNow.TrySetResult();
                _wake.TrySetResult();
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(25);
        }
    }

    private async Task SendLoopAsync()
    {
        while (true)
        {
            Task wake;
            lock (_gate)
            {
                if (_lines.Count == 0 && _dropped == 0)
                    wake = _wake.Task;
                else
                    wake = Task.CompletedTask;
            }

            if (!await WaitAsync(wake, TimeSpan.FromSeconds(_settings.IdleSeconds)))
            {
                lock (_gate)
                {
                    if (_lines.Count == 0 && _dropped == 0)
                    {
                        _completed = true;
                        return;
                    }
                }
            }

            if (_sentFirst || !_settings.EagerlyEmitFirstEvent)
            {
                Task sendNow;
                lock (_gate)
                    sendNow = _sendNow.Task;
                await WaitAsync(sendNow, TimeSpan.FromSeconds(_settings.PeriodSeconds));
            }

            var batch = TakeBatch();
            if (batch.Count == 0)
                continue;

            try
            {
                await _send(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not ship {Count} log lines for job {JobId}", batch.Count, _jobId);
            }
            finally
            {
                lock (_gate)
                    _sending = false;
            }

            _sentFirst = true;
        }
    }

    private List<TLogEntry> TakeBatch()
    {
        lock (_gate)
        {
            var batch = new List<TLogEntry>();
            long bytes = 0;
            while (_lines.Count > 0
                   && batch.Count < _settings.BatchSizeLimit
                   && (batch.Count == 0 || bytes + (_lines.Peek().Message?.Length ?? 0) <= _settings.BatchBytesLimit))
            {
                var line = _lines.Dequeue();
                bytes += line.Message?.Length ?? 0;
                batch.Add(line);
            }

            _pendingBytes -= bytes;

            if (_lines.Count == 0 && _dropped > 0 && _lastLine is { } last)
            {
                batch.Add(DroppedNotice(last, _dropped));
                _dropped = 0;
            }

            if (_lines.Count < _settings.QueueWarningThreshold / 2)
                _warned = false;

            var stamp = DateTimeOffset.UtcNow;
            foreach (var line in batch)
                line.BatchTimeStamp = stamp;

            _sending = batch.Count > 0;
            if (_lines.Count < _settings.BatchSizeLimit && _pendingBytes < _settings.BatchBytesLimit)
                _sendNow = NewSignal();
            if (_lines.Count == 0)
                _wake = NewSignal();

            return batch;
        }
    }

    private static TLogEntry DroppedNotice(TLogEntry last, int dropped) => new()
    {
        JobId = last.JobId,
        ModuleId = last.ModuleId,
        StackName = last.StackName,
        NamespaceName = last.NamespaceName,
        ModuleName = last.ModuleName,
        TaskName = last.TaskName,
        Timestamp = DateTimeOffset.UtcNow,
        Level = LogEventLevel.Warning,
        Message = $"{dropped} log lines were dropped because the runner could not ship them fast enough."
    };

    /// <summary>Waits for <paramref name="signal"/> or the timeout; true if the signal came first.</summary>
    private static async Task<bool> WaitAsync(Task signal, TimeSpan timeout)
    {
        using var cancel = new CancellationTokenSource();
        var delay = Task.Delay(timeout, cancel.Token);
        var first = await Task.WhenAny(signal, delay);
        await cancel.CancelAsync();
        return first == signal;
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
