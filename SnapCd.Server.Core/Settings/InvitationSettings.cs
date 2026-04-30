namespace SnapCd.Server.Core.Settings;

public class InvitationSettings
{
    public int ExpirationDays { get; set; } = 30;
    public bool AutoDeleteIncompleteUsers { get; set; } = true;
    public bool RequireEmailVerification { get; set; } = true;
    public string CleanupJobCron { get; set; } = "0 0 * * *"; // Daily at midnight
}