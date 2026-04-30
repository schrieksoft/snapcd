using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Email;

public interface IEmailSenderWrapper : IEmailSender<User>
{
    public Task<string> SendConfirmationLinkWithResponseAsync(User user, string email, string confirmationLink);
    public Task<string> SendPasswordResetLinkWithResponseAsync(User user, string email, string resetLink);
    public Task<string> SendPasswordResetCodeWithResponseAsync(User user, string email, string resetCode);
    public Task SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink, int expirationDays = 30);
    public Task SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail);
    public Task SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message);
    public Task SendEmailAsync(string toEmail, string subject, string message);
}