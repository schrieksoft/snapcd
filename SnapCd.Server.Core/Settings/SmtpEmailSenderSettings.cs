namespace SnapCd.Server.Core.Settings;

public class SmtpEmailSenderSettings
{
    public string SmtpHost { get; set; } = "smtp-relay.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public bool UseStartTls { get; set; } = true;
}