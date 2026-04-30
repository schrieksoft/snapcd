namespace SnapCd.Server.Core.Settings;

public class LicenseSettings
{
    public string LicenseServerBaseUrl { get; set; } = "https://snapcd.io";

    public string RefreshJobCron { get; set; } = "0 3 * * *";

    public int RefreshWithinDaysOfExpiry { get; set; } = 3;
}
