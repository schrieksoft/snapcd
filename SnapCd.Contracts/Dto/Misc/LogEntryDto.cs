using Serilog.Events;

namespace SnapCd.Contracts.Dto.Misc;

public class LogEntryDto
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public DateTimeOffset BatchTimeStamp { get; set; }

    public Guid? StackId { get; set; }


    public Guid? NamespaceId { get; set; }

    public Guid ModuleId { get; set; }

    public string StackName { get; set; } = null!;

    public string NamespaceName { get; set; } = null!;
    public string? ModuleName { get; set; }

    public LogEventLevel Level { get; set; }
    public string Message { get; set; } = null!;

    public string TaskName { get; set; } = null!;

    public Dictionary<string, string>? Tags { get; set; }

    public LogSource Source { get; set; } = LogSource.Runner;
}