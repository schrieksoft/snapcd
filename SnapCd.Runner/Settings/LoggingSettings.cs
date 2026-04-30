using Serilog.Events;

namespace SnapCd.Runner.Settings;

public class LoggingSettings
{
    public LogEventLevel SystemDefaultLogLevel { get; set; } = LogEventLevel.Error;
    public LogEventLevel SnapCdDefaultLogLevel { get; set; } = LogEventLevel.Information;

    public Dictionary<string, LogEventLevel> LogLevelOverrides { get; set; } = new();

    public int BatchSizeLimit { get; set; } = 50;
    public int PeriodSeconds { get; set; } = 5;
    public bool EarlyEmitFirstEvent { get; set; } = true;
}