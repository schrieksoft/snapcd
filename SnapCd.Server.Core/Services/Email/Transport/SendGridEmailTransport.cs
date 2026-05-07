using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email.Transport;

public class SendGridEmailTransport : IEmailTransport
{
    private readonly ILogger<SendGridEmailTransport> _logger;
    private readonly SendGridEmailTransportSettings _settings;

    public SendGridEmailTransport(
        IOptions<SendGridEmailTransportSettings> options,
        ILogger<SendGridEmailTransport> logger)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("SendGrid ApiKey is not configured");

        var client = new SendGridClient(_settings.ApiKey);
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            PlainTextContent = plainTextContent ?? htmlContent,
            HtmlContent = htmlContent,
        };
        msg.AddTo(new EmailAddress(toEmail));
        msg.SetClickTracking(false, false);

        var response = await client.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email to {Email} queued successfully via SendGrid.", toEmail);
            return true;
        }

        var body = await response.Body.ReadAsStringAsync();
        _logger.LogError("SendGrid send failed for {Email}. StatusCode: {Status}. Body: {Body}", toEmail, response.StatusCode, body);
        throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" with SendGrid. StatusCode: {response.StatusCode}. Body: {body}");
    }
}
