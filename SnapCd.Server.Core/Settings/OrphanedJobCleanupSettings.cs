namespace SnapCd.Server.Core.Settings;

public class OrphanedJobCleanupSettings
{
    public string CleanupCronExpression { get; set; } = "*/10 * * * *"; // Every 10 minutes
}
