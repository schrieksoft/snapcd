using Microsoft.AspNetCore.Identity.UI.Services;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Email;

// Update with a real email implementation when needed for password reset and invitation emails.
public class IdentityNoOpEmailSender : IEmailSenderWrapper
{
    private readonly IEmailSender _emailSender = new SimpleNoOpEmailSender();

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        return _emailSender.SendEmailAsync(email, "Confirm your email", BuildConfirmationLinkMessage(confirmationLink));
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        return _emailSender.SendEmailAsync(email, "Reset your password", BuildConfirmationLinkMessage(resetLink));
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        return _emailSender.SendEmailAsync(email, "Reset your password", BuildConfirmationLinkMessage(resetCode));
    }


    private string BuildConfirmationLinkMessage(string confirmationLink)
    {
        return $"Please confirm your account by following this link: {confirmationLink}";
    }

    private string BuildPasswordResetLinkMessage(string resetLink)
    {
        return $"Please reset your password by following this link: {resetLink}";
    }

    private string BuildPasswordResetCodeMessage(string resetCode)
    {
        return $"Please reset your password using the following code: {resetCode}";
    }

    public async Task<string> SendConfirmationLinkWithResponseAsync(User user, string email, string confirmationLink)
    {
        try
        {
            await SendConfirmationLinkAsync(user, email, confirmationLink);
            return
                $"Your Snap CD Server does not have an Email Sender configured. In order to complete user registration, please provide user with the following instructions: \"{BuildConfirmationLinkMessage(confirmationLink)}\"";
        }
        catch (Exception ex)
        {
            return $"Failed to use NoOp email sender to 'send' invitation email. Error: {ex.Message}";
        }
    }

    public async Task<string> SendPasswordResetLinkWithResponseAsync(User user, string email, string resetLink)
    {
        try
        {
            await SendPasswordResetLinkAsync(user, email, resetLink);
            return
                $"Your Snap CD Server does not have an Email Sender configured. In order to complete user registration, please provide user with the following instructions:\n\n{BuildPasswordResetLinkMessage(resetLink)}";
        }
        catch (Exception ex)
        {
            return $"Failed to use NoOp email sender to 'send' password reset email. Error: {ex.Message}";
        }
    }

    public async Task<string> SendPasswordResetCodeWithResponseAsync(User user, string email, string resetCode)
    {
        try
        {
            await SendPasswordResetCodeAsync(user, email, resetCode);
            return
                $"Your Snap CD Server does not have an Email Sender configured. In order to complete password reset, please provide user with the following instructions:\n\n{BuildPasswordResetCodeMessage(resetCode)}";
        }
        catch (Exception ex)
        {
            return $"Failed to use NoOp email sender to 'send' password reset email. Error: {ex.Message}";
        }
    }

    public Task SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink, int expirationDays = 30)
    {
        var message = $"You have been invited to join {organizationName} by {inviterName} ({inviterEmail}). Accept or decline invitation: {invitationLink}. Expires in {expirationDays} days.";
        return _emailSender.SendEmailAsync(email, $"{inviterName} invited you to {organizationName}", message);
    }

    public Task SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail)
    {
        var emailSubject = EmailTemplateHelper.GetContactFormSubject(fromName, subject);
        var plainText = EmailTemplateHelper.GetContactFormPlainText(fromName, fromEmail, subject, message);
        return _emailSender.SendEmailAsync(toEmail, emailSubject, plainText);
    }

    public Task SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message)
    {
        var emailSubject = EmailTemplateHelper.GetContactFormConfirmationSubject(subject);
        var plainText = EmailTemplateHelper.GetContactFormConfirmationPlainText(toName, subject, message);
        return _emailSender.SendEmailAsync(toEmail, emailSubject, plainText);
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        return _emailSender.SendEmailAsync(toEmail, subject, message);
    }
}