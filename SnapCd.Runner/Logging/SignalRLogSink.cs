using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Services;


namespace SnapCd.Runner.Logging;

public class SignalRLogSink : IBatchedLogEventSink
{
    private readonly RunnerHubConnection _hubConnection;

    public SignalRLogSink(RunnerHubConnection hubConnection)
    {
        _hubConnection = hubConnection;
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

            if (LogMessageHelper.GetGuidProperty(logEvent, nameof(LogEntryDto.ModuleId)) == Guid.Empty)
                continue;

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
                BatchTimeStamp = batchTimeStamp,
                Source = LogSource.Runner
            };

            logEntries.Add(logEntry);
        }

        await _hubConnection.SendLogsAsync(logEntries);
    }


    public Task OnEmptyBatchAsync()
    {
        // Optional: implement any behavior you want when no log events are available in a batch.
        return Task.CompletedTask;
    }
}