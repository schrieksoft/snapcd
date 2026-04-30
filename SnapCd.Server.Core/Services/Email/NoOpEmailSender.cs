using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Email;

public class NoOpEmailSender : IEmailSender<User>
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        _logger.LogInformation("NoOp email sender: Would send confirmation link to {Email} with link {Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        _logger.LogInformation("NoOp email sender: Would send password reset link to {Email} with link {Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        _logger.LogInformation("NoOp email sender: Would send password reset code to {Email} with code {Code}", email, resetCode);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        _logger.LogInformation("NoOp email sender: Would send email to {Email} with subject '{Subject}'", toEmail, subject);
        return Task.CompletedTask;
    }
}