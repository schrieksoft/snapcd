namespace SnapCd.Server.Core.Settings;

public class SendGridEmailSenderSettings
{
    public string ApiKey { get; set; } = null!;
    public string FromEmail { get; set; } = null!;

    public string FromName { get; set; } = null!;
}