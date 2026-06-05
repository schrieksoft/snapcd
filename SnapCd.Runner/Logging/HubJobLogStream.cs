// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Logging;

/// <summary>
/// Default <see cref="IJobLogStream"/>. Owns a private Serilog logger configured with
/// <see cref="PeriodicBatchingSink"/> wrapping <see cref="SignalRLogSink"/>. Serilog is
/// scoped to this class only — the rest of the runner is on vanilla MEL, configured via
/// the standard <c>Logging</c> section of appsettings (level filtering happens upstream at
/// <see cref="RunnerTaskContext"/> emit sites, not here).
///
/// Robustness inherited from Serilog: in-process queueing, period/size-driven batching,
/// eager first-event emit, per-sink exception isolation, drain-on-dispose. Outage buffering
/// + reconnect-driven flush + per-retry exception handling live downstream in
/// <see cref="RunnerHubConnection.SendLogsAsync"/>.
/// </summary>
public sealed class HubJobLogStream : IJobLogStream, IAsyncDisposable
{
    private readonly Logger _shipper;
    private readonly PeriodicBatchingSink _batchSink;

    public HubJobLogStream(RunnerHubConnection hub, IOptions<JobLogStreamSettings> settings)
    {
        var s = settings.Value;
        _batchSink = new PeriodicBatchingSink(
            new SignalRLogSink(hub),
            new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = s.BatchSizeLimit,
                Period = TimeSpan.FromSeconds(s.PeriodSeconds),
                EagerlyEmitFirstEvent = s.EagerlyEmitFirstEvent,
            });

        _shipper = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_batchSink)
            .CreateLogger();
    }

    public Task EmitAsync(JobLogEnvelope envelope, string message, CancellationToken ct = default)
    {
        // ForContext attaches properties to the next event only (no AsyncLocal). SignalRLogSink
        // reads these property names off LogEvent.Properties via LogMessageHelper to build the DTO.
        _shipper
            .ForContext(nameof(LogEntryDto.JobId), envelope.JobId)
            .ForContext(nameof(LogEntryDto.ModuleId), envelope.ModuleId)
            .ForContext(nameof(LogEntryDto.StackName), envelope.StackName)
            .ForContext(nameof(LogEntryDto.NamespaceName), envelope.NamespaceName)
            .ForContext(nameof(LogEntryDto.ModuleName), envelope.ModuleName)
            .ForContext(nameof(LogEntryDto.TaskName), envelope.TaskName)
            .ForContext(nameof(LogEntryDto.Message), message)
            .Write(MapLevel(envelope.Level), message);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Disposing the Serilog logger flushes the PeriodicBatchingSink — final drain on shutdown.
        await _shipper.DisposeAsync();
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
