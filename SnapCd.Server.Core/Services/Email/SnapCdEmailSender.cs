using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Email.Transport;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email;

/// <summary>
/// The <see cref="ISnapCdEmailSender"/> implementation. Owns subject strings and body
/// composition via <see cref="EmailTemplateHelper"/>; delegates delivery to the injected
/// <see cref="IEmailTransport"/>.
/// </summary>
public class SnapCdEmailSender : ISnapCdEmailSender
{
    private readonly IEmailTransport _transport;
    private readonly ServerSettings _serverSettings;

    public SnapCdEmailSender(
        IEmailTransport transport,
        IOptions<ServerSettings> serverSettings)
    {
        _transport = transport;
        _serverSettings = serverSettings.Value;
    }

    public Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default) => _transport.IsDeliveryActiveAsync(ct);

    // --- IEmailSender<User> contract methods (return Task per Identity contract; outcome discarded). ---

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
        => SendConfirmationLinkInternalAsync(email, confirmationLink);

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
        => SendPasswordResetLinkInternalAsync(email, resetLink);

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var html = EmailTemplateHelper.GeneratePasswordResetCodeEmail(resetCode);
        var plain = EmailTemplateHelper.GetPasswordResetCodePlainText(resetCode);
        return _transport.SendAsync(email, "Reset Your Snap CD Password", html, plain);
    }

    // --- Snapcd-specific methods (outcome-returning). ---

    public Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        => _transport.SendAsync(toEmail, subject, message);

    public Task<bool> SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink, int expirationDays = 30)
    {
        var html = EmailTemplateHelper.GenerateOrganizationInvitationEmail(invitationLink, organizationName, inviterName, inviterEmail, _serverSettings.Host, expirationDays);
        var plain = EmailTemplateHelper.GetOrganizationInvitationPlainText(organizationName, inviterName, invitationLink);
        return _transport.SendAsync(email, $"{inviterName} invited you to {organizationName}", html, plain);
    }

    public Task<bool> SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail)
    {
        var subjectText = EmailTemplateHelper.GetContactFormSubject(fromName, subject);
        var html = EmailTemplateHelper.GenerateContactFormEmail(fromName, fromEmail, subject, message, _serverSettings.Host);
        var plain = EmailTemplateHelper.GetContactFormPlainText(fromName, fromEmail, subject, message);
        return _transport.SendAsync(toEmail, subjectText, html, plain);
    }

    public Task<bool> SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message)
    {
        var subjectText = EmailTemplateHelper.GetContactFormConfirmationSubject(subject);
        var html = EmailTemplateHelper.GenerateContactFormConfirmationEmail(toName, subject, message, _serverSettings.Host);
        var plain = EmailTemplateHelper.GetContactFormConfirmationPlainText(toName, subject, message);
        return _transport.SendAsync(toEmail, subjectText, html, plain);
    }

    public Task<bool> SendPasswordResetLinkWithOutcomeAsync(User user, string email, string resetLink)
        => SendPasswordResetLinkInternalAsync(email, resetLink);

    // --- Internals (shared between the void-returning Identity contract methods and outcome-returning companions). ---

    private Task<bool> SendConfirmationLinkInternalAsync(string email, string confirmationLink)
    {
        var html = EmailTemplateHelper.GenerateEmailConfirmationEmail(confirmationLink, _serverSettings.Host);
        var plain = EmailTemplateHelper.GetPlainTextVersion(
            "Email Confirmation Required",
            confirmationLink,
            "Thank you for registering with Snap CD. Please confirm your email address by clicking the link below.");
        return _transport.SendAsync(email, "Confirm Your Snap CD Email", html, plain);
    }

    private Task<bool> SendPasswordResetLinkInternalAsync(string email, string resetLink)
    {
        var html = EmailTemplateHelper.GeneratePasswordResetEmail(resetLink, _serverSettings.Host);
        var plain = EmailTemplateHelper.GetPlainTextVersion(
            "Password Reset Request",
            resetLink,
            "We received a request to reset your Snap CD account password. Click the link to reset your password.");
        return _transport.SendAsync(email, "Reset Your Snap CD Password", html, plain);
    }
}
