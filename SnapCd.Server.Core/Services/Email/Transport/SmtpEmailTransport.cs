// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email.Transport;

public class SmtpEmailTransport : IEmailTransport
{
    private readonly ILogger<SmtpEmailTransport> _logger;
    private readonly SmtpEmailTransportSettings _settings;

    public SmtpEmailTransport(
        IOptions<SmtpEmailTransportSettings> options,
        ILogger<SmtpEmailTransport> logger)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (string.IsNullOrEmpty(_settings.SmtpHost))
            throw new InvalidOperationException("SMTP host is not configured");

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl || _settings.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
        };
        message.To.Add(toEmail);

        if (plainTextContent is not null)
        {
            // Multipart/alternative: plaintext as body, HTML as alternate view.
            message.Body = plainTextContent;
            message.IsBodyHtml = false;
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlContent, Encoding.UTF8, "text/html"));
        }
        else
        {
            message.Body = htmlContent;
            message.IsBodyHtml = true;
        }

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email to {Email} sent successfully via SMTP.", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for {Email}: {Error}", toEmail, ex.Message);
            throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" via SMTP. Error: {ex.Message}", ex);
        }
    }
}
