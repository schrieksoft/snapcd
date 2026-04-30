namespace SnapCd.Server.Core.Settings;

public class AmazonSesEmailSenderSettings
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string Region { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public bool UseDefaultCredentials { get; set; } = true;
}