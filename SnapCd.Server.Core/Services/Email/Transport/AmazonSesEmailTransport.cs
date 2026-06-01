// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email.Transport;

public class AmazonSesEmailTransport : IEmailTransport
{
    private readonly ILogger<AmazonSesEmailTransport> _logger;
    private readonly AmazonSesEmailTransportSettings _settings;

    public AmazonSesEmailTransport(
        IOptions<AmazonSesEmailTransportSettings> options,
        ILogger<AmazonSesEmailTransport> logger)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (string.IsNullOrEmpty(_settings.Region))
            throw new InvalidOperationException("Amazon SES Region is not configured");

        var plainText = plainTextContent ?? htmlContent;
        var regionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region);

        AmazonSimpleEmailServiceClient client;
        if (_settings.UseDefaultCredentials)
        {
            client = new AmazonSimpleEmailServiceClient(regionEndpoint);
        }
        else
        {
            if (string.IsNullOrEmpty(_settings.AccessKey))
                throw new InvalidOperationException("Amazon SES AccessKey is not configured");
            if (string.IsNullOrEmpty(_settings.SecretKey))
                throw new InvalidOperationException("Amazon SES SecretKey is not configured");

            client = new AmazonSimpleEmailServiceClient(_settings.AccessKey, _settings.SecretKey, regionEndpoint);
        }

        using (client)
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
                        Html = new Content { Charset = "UTF-8", Data = htmlContent },
                        Text = new Content { Charset = "UTF-8", Data = plainText },
                    }
                }
            };

            try
            {
                var response = await client.SendEmailAsync(sendRequest);
                _logger.LogInformation("Email to {Email} queued successfully! MessageId: {MessageId}", toEmail, response.MessageId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Error}", toEmail, ex.Message);
                throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" with Amazon SES. Error: {ex.Message}", ex);
            }
        }
    }
}
