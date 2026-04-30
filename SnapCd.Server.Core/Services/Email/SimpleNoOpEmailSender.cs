using Microsoft.AspNetCore.Identity.UI.Services;

namespace SnapCd.Server.Core.Services.Email;

public class SimpleNoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // No-op implementation for development/testing
        return Task.CompletedTask;
    }
}