using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email;

public class SendGridEmailSender(
    IOptions<SendGridEmailSenderSettings> optionsAccessor,
    IOptions<ServerSettings> ServerSettings,
    ILogger<SendGridEmailSender> logger) : IEmailSenderWrapper
{
    private readonly ILogger _logger = logger;
    private readonly ServerSettings _ServerSettings = ServerSettings.Value;

    public SendGridEmailSenderSettings Settings { get; } = optionsAccessor.Value;

    public Task SendConfirmationLinkAsync(User user, string email,
        string confirmationLink)
    {
        return SendEmailAsync(email, "Confirm your email",
            $"You have been invited to use your organization's Snap CD deployment. Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    public Task SendPasswordResetLinkAsync(User user, string email,
        string resetLink)
    {
        return SendEmailAsync(email, "Reset your password",
            $"A password reset has been initialised for your Snap CD user. Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    public Task SendPasswordResetCodeAsync(User user, string email,
        string resetCode)
    {
        return SendEmailAsync(email, "Reset your password",
            $"A password reset has been initialised for your Snap CD user. Please reset your password using the following code: {resetCode}");
    }


    public async Task<string> SendConfirmationLinkWithResponseAsync(User user, string email, string confirmationLink)
    {
        await SendConfirmationLinkAsync(user, email, confirmationLink);
        return "";
    }

    public async Task<string> SendPasswordResetLinkWithResponseAsync(User user, string email, string resetLink)
    {
        await SendPasswordResetLinkAsync(user, email, resetLink);
        return "";
    }

    public async Task<string> SendPasswordResetCodeWithResponseAsync(User user, string email, string resetCode)
    {
        await SendPasswordResetCodeAsync(user, email, resetCode);
        return "";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrEmpty(Settings.ApiKey)) throw new Exception("Null EmailAuthKey");

        await Execute(Settings.ApiKey, subject, message, toEmail);
    }

    public async Task Execute(string apiKey, string subject, string message, string toEmail)
    {
        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage
        {
            From = new EmailAddress(Settings.FromEmail, Settings.FromName),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        msg.AddTo(new EmailAddress(toEmail));

        // Disable click tracking.
        // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email to {Email} queued successfully!", toEmail);
        }
        else
        {
            _logger.LogInformation("Failed to send Email to {Email}", toEmail);
            throw new Exception($"Failed to send Email to \"{toEmail}\" with SendGrid. StatusCode: {response.StatusCode}. Body: {response.Body}");
        }
    }

    public async Task SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink,
        int expirationDays = 30)
    {
        var htmlContent = EmailTemplateHelper.GenerateOrganizationInvitationEmail(invitationLink, organizationName, inviterName, inviterEmail, _ServerSettings.Host, expirationDays);
        await SendEmailAsync(email, $"{inviterName} invited you to {organizationName}", htmlContent);
    }

    public async Task SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail)
    {
        var subjectText = EmailTemplateHelper.GetContactFormSubject(fromName, subject);
        var htmlContent = EmailTemplateHelper.GenerateContactFormEmail(fromName, fromEmail, subject, message, _ServerSettings.Host);
        await SendEmailAsync(toEmail, subjectText, htmlContent);
    }

    public async Task SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message)
    {
        var subjectText = EmailTemplateHelper.GetContactFormConfirmationSubject(subject);
        var htmlContent = EmailTemplateHelper.GenerateContactFormConfirmationEmail(toName, subject, message, _ServerSettings.Host);
        await SendEmailAsync(toEmail, subjectText, htmlContent);
    }
}