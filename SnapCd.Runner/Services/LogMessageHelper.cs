using Serilog.Events;

namespace SnapCd.Runner.Services;

public class LogMessageHelper
{
    public static string GetStringProperty(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value)
            ? value.ToString().Trim('"')
            : string.Empty;
    }

    public static Guid GetGuidProperty(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value) &&
               Guid.TryParse(value.ToString().Trim('"'), out var guid)
            ? guid
            : Guid.Empty;
    }

    public static string TrimQuotes(string value)
    {
        if (value.StartsWith('"') && value.EndsWith('"'))
            return value.Substring(1, value.Length - 2);
        return value;
    }
}