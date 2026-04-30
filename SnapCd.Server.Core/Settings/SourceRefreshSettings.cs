namespace SnapCd.Server.Core.Settings;

public class SourceRefreshSettings
{
    public string RefreshIntervalCronExpression { get; set; } = "*/5 * * * *"; // Every 5 minutes

    public int TimeoutSeconds { get; set; } = 120; // two minutes
}