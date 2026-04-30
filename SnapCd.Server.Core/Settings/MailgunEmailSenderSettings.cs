namespace SnapCd.Server.Core.Settings;

public class MailgunEmailSenderSettings
{
    public string ApiKey { get; set; } = null!;
    public string Domain { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
}