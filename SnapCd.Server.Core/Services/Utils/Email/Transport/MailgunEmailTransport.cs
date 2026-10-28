// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email.Transport;

public class MailgunEmailTransport : IEmailTransport
{
    private readonly ILogger<MailgunEmailTransport> _logger;
    private readonly MailgunEmailTransportSettings _settings;

    public MailgunEmailTransport(
        IOptions<MailgunEmailTransportSettings> options,
        ILogger<MailgunEmailTransport> logger)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("Mailgun ApiKey is not configured");
        if (string.IsNullOrEmpty(_settings.Domain))
            throw new InvalidOperationException("Mailgun Domain is not configured");

        var options = new RestClientOptions("https://api.mailgun.net")
        {
            Authenticator = new HttpBasicAuthenticator("api", _settings.ApiKey),
        };
        using var client = new RestClient(options);

        var request = new RestRequest($"v3/{_settings.Domain}/messages", Method.Post);
        request.AddParameter("from", $"{_settings.FromName} <{_settings.FromEmail}>");
        request.AddParameter("to", toEmail);
        request.AddParameter("subject", subject);
        request.AddParameter("html", htmlContent);
        if (plainTextContent is not null)
            request.AddParameter("text", plainTextContent);

        var response = await client.ExecuteAsync(request);
        if (response.IsSuccessful)
        {
            _logger.LogInformation("Email to {Email} queued successfully via Mailgun.", toEmail);
            return true;
        }

        _logger.LogError("Mailgun send failed for {Email}. Status: {Status}. Body: {Body}", toEmail, response.StatusCode, response.Content);
        throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" with Mailgun. Status: {response.StatusCode}. Body: {response.Content}");
    }
}
