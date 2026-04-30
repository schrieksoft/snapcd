using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email;

public class AmazonSesEmailSender : IEmailSender<User>
{
    private readonly ILogger<AmazonSesEmailSender> _logger;
    private readonly ServerSettings _serverSettings;
    private readonly AmazonSesEmailSenderSettings _settings;

    public AmazonSesEmailSender(
        IOptions<AmazonSesEmailSenderSettings> optionsAccessor,
        IOptions<ServerSettings> serverSettings,
        ILogger<AmazonSesEmailSender> logger)
    {
        _logger = logger;
        _serverSettings = serverSettings.Value;
        _settings = optionsAccessor.Value;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var htmlContent = EmailTemplateHelper.GenerateEmailConfirmationEmail(confirmationLink, _serverSettings.Host);
        var plainTextContent = EmailTemplateHelper.GetPlainTextVersion(
            "Email Confirmation Required",
            confirmationLink,
            "Thank you for registering with Snap CD. Please confirm your email address by clicking the link below."
        );

        return SendEmailWithTemplateAsync(email, "Confirm Your Snap CD Email", plainTextContent, htmlContent);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var htmlContent = EmailTemplateHelper.GeneratePasswordResetEmail(resetLink, _serverSettings.Host);
        var plainTextContent = EmailTemplateHelper.GetPlainTextVersion(
            "Password Reset Request",
            resetLink,
            "We received a request to reset your Snap CD account password. Click the link to reset your password."
        );

        return SendEmailWithTemplateAsync(email, "Reset Your Snap CD Password", plainTextContent, htmlContent);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        // For reset codes, we'll use a simpler format since it's not a link
        var htmlMessage = $@"
            <p>A password reset has been requested for your Snap CD account.</p>
            <p>Your password reset code is: <strong style='font-size: 18px; color: #000;'>{resetCode}</strong></p>
            <p>Please enter this code to reset your password.</p>";

        var plainMessage = $"A password reset has been requested for your Snap CD account.\n\nYour password reset code is: {resetCode}\n\nPlease enter this code to reset your password.";

        return SendEmailWithTemplateAsync(email, "Reset Your Snap CD Password", plainMessage, htmlMessage);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        await SendEmailWithTemplateAsync(toEmail, subject, message, message);
    }

    public async Task SendEmailWithTemplateAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
    {
        if (string.IsNullOrEmpty(_settings.Region))
            throw new InvalidOperationException("Amazon SES Region is not configured");

        if (_settings.UseDefaultCredentials)
        {
            await ExecuteWithDefaultCredentials(_settings.Region, subject, plainTextContent, htmlContent, toEmail);
        }
        else
        {
            if (string.IsNullOrEmpty(_settings.AccessKey))
                throw new InvalidOperationException("Amazon SES AccessKey is not configured");
            if (string.IsNullOrEmpty(_settings.SecretKey))
                throw new InvalidOperationException("Amazon SES SecretKey is not configured");

            await ExecuteWithExplicitCredentials(_settings.AccessKey, _settings.SecretKey, _settings.Region, subject, plainTextContent, htmlContent, toEmail);
        }
    }

    private async Task ExecuteWithDefaultCredentials(string region, string subject, string plainTextMessage, string htmlMessage, string toEmail)
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        using var client = new AmazonSimpleEmailServiceClient(regionEndpoint);

        await SendEmailInternal(client, subject, plainTextMessage, htmlMessage, toEmail);
    }

    private async Task ExecuteWithExplicitCredentials(string accessKey, string secretKey, string region, string subject, string plainTextMessage, string htmlMessage, string toEmail)
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        using var client = new AmazonSimpleEmailServiceClient(accessKey, secretKey, regionEndpoint);

        await SendEmailInternal(client, subject, plainTextMessage, htmlMessage, toEmail);
    }

    private async Task SendEmailInternal(AmazonSimpleEmailServiceClient client, string subject, string plainTextMessage, string htmlMessage, string toEmail)
    {
        var sendRequest = new SendEmailRequest
        {
            Source = $"{_settings.FromName} <{_settings.FromEmail}>",
            Destination = new Destination
            {
                ToAddresses = new List<string> { toEmail }
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlMessage
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = plainTextMessage
                    }
                }
            }
        };

        try
        {
            var response = await client.SendEmailAsync(sendRequest);
            _logger.LogInformation("Email to {Email} queued successfully! MessageId: {MessageId}", toEmail, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Error}", toEmail, ex.Message);
            throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" with Amazon SES. Error: {ex.Message}", ex);
        }
    }
}