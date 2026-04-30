using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email;

public class AmazonSesEmailSenderWrapper : IEmailSenderWrapper
{
    private readonly AmazonSesEmailSender _amazonSesEmailSender;
    private readonly ServerSettings _ServerSettings;

    public AmazonSesEmailSenderWrapper(
        IOptions<AmazonSesEmailSenderSettings> optionsAccessor,
        IOptions<ServerSettings> ServerSettings,
        ILogger<AmazonSesEmailSender> logger)
    {
        _amazonSesEmailSender = new AmazonSesEmailSender(optionsAccessor, ServerSettings, logger);
        _ServerSettings = ServerSettings.Value;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        return _amazonSesEmailSender.SendConfirmationLinkAsync(user, email, confirmationLink);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        return _amazonSesEmailSender.SendPasswordResetLinkAsync(user, email, resetLink);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        return _amazonSesEmailSender.SendPasswordResetCodeAsync(user, email, resetCode);
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        return _amazonSesEmailSender.SendEmailAsync(toEmail, subject, message);
    }

    public async Task<string> SendConfirmationLinkWithResponseAsync(User user, string email, string confirmationLink)
    {
        try
        {
            await SendConfirmationLinkAsync(user, email, confirmationLink);
            return "Email confirmation link sent successfully";
        }
        catch (Exception ex)
        {
            return $"Failed to send email confirmation link: {ex.Message}";
        }
    }

    public async Task<string> SendPasswordResetLinkWithResponseAsync(User user, string email, string resetLink)
    {
        try
        {
            await SendPasswordResetLinkAsync(user, email, resetLink);
            return "Password reset link sent successfully";
        }
        catch (Exception ex)
        {
            return $"Failed to send password reset link: {ex.Message}";
        }
    }

    public async Task<string> SendPasswordResetCodeWithResponseAsync(User user, string email, string resetCode)
    {
        try
        {
            await SendPasswordResetCodeAsync(user, email, resetCode);
            return "Password reset code sent successfully";
        }
        catch (Exception ex)
        {
            return $"Failed to send password reset code: {ex.Message}";
        }
    }

    public Task SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink, int expirationDays = 30)
    {
        var htmlContent = EmailTemplateHelper.GenerateOrganizationInvitationEmail(invitationLink, organizationName, inviterName, inviterEmail, _ServerSettings.Host, expirationDays);
        var plainTextContent = EmailTemplateHelper.GetOrganizationInvitationPlainText(organizationName, inviterName, invitationLink);

        return _amazonSesEmailSender.SendEmailWithTemplateAsync(email, $"{inviterName} invited you to {organizationName}", plainTextContent, htmlContent);
    }

    public Task SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail)
    {
        var subjectText = EmailTemplateHelper.GetContactFormSubject(fromName, subject);
        var htmlContent = EmailTemplateHelper.GenerateContactFormEmail(fromName, fromEmail, subject, message, _ServerSettings.Host);
        var plainTextContent = EmailTemplateHelper.GetContactFormPlainText(fromName, fromEmail, subject, message);

        return _amazonSesEmailSender.SendEmailWithTemplateAsync(toEmail, subjectText, plainTextContent, htmlContent);
    }

    public Task SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message)
    {
        var subjectText = EmailTemplateHelper.GetContactFormConfirmationSubject(subject);
        var htmlContent = EmailTemplateHelper.GenerateContactFormConfirmationEmail(toName, subject, message, _ServerSettings.Host);
        var plainTextContent = EmailTemplateHelper.GetContactFormConfirmationPlainText(toName, subject, message);

        return _amazonSesEmailSender.SendEmailWithTemplateAsync(toEmail, subjectText, plainTextContent, htmlContent);
    }
}