using Serilog;
using Serilog.Configuration;

namespace SnapCd.Runner.Logging;

public static class SerilogExtensions
{
    public static LoggerConfiguration CustomConsole(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        IFormatProvider? formatProvider = null)
    {
        return loggerSinkConfiguration.Sink(new CustomConsoleSink(formatProvider));
    }
}