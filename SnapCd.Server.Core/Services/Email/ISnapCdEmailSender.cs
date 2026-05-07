using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Email;

public interface ISnapCdEmailSender : IEmailSender<User>
{
    /// <summary>
    /// Predictive: returns true if a send right now would actually deliver. Use for UI decisions
    /// made before any send. After a send, branch on the bool returned by the send method
    /// itself — that is authoritative.
    /// </summary>
    Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns true when the send actually delivered, false when it was no-op'd.
    /// </summary>
    Task<bool> SendOrganizationInvitationAsync(string email, string organizationName, string inviterName, string inviterEmail, string invitationLink, int expirationDays = 30);

    Task<bool> SendContactFormAsync(string fromName, string fromEmail, string subject, string message, string toEmail);
    Task<bool> SendContactFormConfirmationAsync(string toName, string toEmail, string subject, string message);
    Task<bool> SendEmailAsync(string toEmail, string subject, string message);

    /// <summary>
    /// Outcome-returning equivalent of <c>IEmailSender&lt;User&gt;.SendPasswordResetLinkAsync</c>.
    /// Returns true when the send actually delivered.
    /// </summary>
    Task<bool> SendPasswordResetLinkWithOutcomeAsync(User user, string email, string resetLink);
}
