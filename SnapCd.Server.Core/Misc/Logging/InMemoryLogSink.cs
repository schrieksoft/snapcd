using System.Text.RegularExpressions;
using Serilog.Events;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Services;
using IBatchedLogEventSink = Serilog.Sinks.PeriodicBatching.IBatchedLogEventSink;

namespace SnapCd.Server.Core.Misc.Logging;

// TODO this currently does not work, seems like lines being crossed between ILogger registration in mass transit and here

public class InMemoryLogSink : IBatchedLogEventSink
{
    private readonly LogService _logStore;

    public InMemoryLogSink(LogService logStore)
    {
        _logStore = logStore;
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        var logEntries = new List<LogEntryDto>();

        var batchTimeStamp = DateTime.UtcNow;


        foreach (var logEvent in events)
        {
            var message = logEvent.Properties.TryGetValue(nameof(LogEntryDto.Message), out var messageValue)
                ? LogMessageHelper.TrimQuotes(Regex.Unescape(messageValue.ToString()))
                : string.Empty;

            var logEntry = new LogEntryDto
            {
                Timestamp = logEvent.Timestamp,
                JobId = LogMessageHelper.GetGuidProperty(logEvent, nameof(LogEntryDto.JobId)),
                StackId = LogMessageHelper.GetGuidProperty(logEvent, nameof(LogEntryDto.StackId)),
                NamespaceId = LogMessageHelper.GetGuidProperty(logEvent, nameof(LogEntryDto.NamespaceId)),
                ModuleId = LogMessageHelper.GetGuidProperty(logEvent, nameof(LogEntryDto.ModuleId)),

                StackName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.StackName)),
                NamespaceName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.NamespaceName)),
                ModuleName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.ModuleName)),
                TaskName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.TaskName)),

                Message = message,
                BatchTimeStamp = batchTimeStamp
            };

            logEntries.Add(logEntry);
        }

        await _logStore.AddLogEntries(logEntries);
    }

    public Task OnEmptyBatchAsync()
    {
        // Optional: implement any behavior you want when no log events are available in a batch.
        return Task.CompletedTask;
    }
}