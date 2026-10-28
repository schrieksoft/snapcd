// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Services;

public class EmailConfirmationService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender<User> _emailSender;
    private readonly SnapCdDbContext _dbContext;
    private readonly IQuotaUsageForEmailConfirmationService _quotaService;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        UserManager<User> userManager,
        IEmailSender<User> emailSender,
        SnapCdDbContext dbContext,
        IQuotaUsageForEmailConfirmationService quotaService,
        ILogger<EmailConfirmationService> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _dbContext = dbContext;
        _quotaService = quotaService;
        _logger = logger;
    }

    /// <summary>
    /// Resends the email confirmation link to the specified email address.
    /// Includes rate limiting based on settings.
    /// </summary>
    /// <param name="email">The email address to send confirmation to</param>
    /// <param name="confirmationUrlTemplate">Template URL with {userId} and {code} placeholders</param>
    /// <returns>Success status and message to display to user</returns>
    public async Task<(bool Success, string Message)> ResendConfirmationEmailAsync(
        string email,
        string confirmationUrlTemplate)
    {
        // Use DbContext directly to find user by email (including unconfirmed users)
        var user = await _dbContext.Users.FirstOrDefaultAsync<User>(u => u.Email == email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist for security reasons
            _logger.LogWarning("Attempted to resend confirmation email for non-existent user: {Email}", email);
            return (false, "If an account exists with this email, a confirmation link has been sent.");
        }

        if (user.EmailConfirmed)
        {
            return (false, "Email is already confirmed. You can sign in now.");
        }

        // Check rate limit
        var quotaResult = await _quotaService.CheckAndRecordAsync(user.Id);
        if (!quotaResult.Allowed)
        {
            _logger.LogWarning("Rate limit exceeded for email confirmation resend: {Email}, UserId: {UserId}", email, user.Id);
            return (false, quotaResult.Message);
        }

        // Generate confirmation token and build URL
        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var confirmationUrl = confirmationUrlTemplate
            .Replace("{userId}", userId)
            .Replace("{code}", code);

        // Send confirmation email
        await _emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(confirmationUrl));

        _logger.LogInformation("Resent confirmation email to {Email}, UserId: {UserId}", email, user.Id);

        return (true, "Confirmation email sent. Please check your inbox and spam folder.");
    }

    /// <summary>
    /// Gets the number of remaining resend attempts for a user.
    /// </summary>
    public async Task<int> GetRemainingAttemptsAsync(Guid userId)
    {
        return await _quotaService.GetRemainingAttemptsAsync(userId);
    }
}
