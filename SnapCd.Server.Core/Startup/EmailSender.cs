// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Email;
using SnapCd.Server.Core.Services.Email.Transport;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

public static class EmailSender
{
    public static IServiceCollection AddSnapCdEmailSender(this IServiceCollection services, ConfigurationManager configuration)
    {
        var provider = configuration["EmailSender:EmailProvider"];

        if (TryRegisterPremiumTransport(services, configuration, provider))
        {
            // Premium configured: layer the licence gate over [premium, noop] keyed transports.
            // Lifetime must be Scoped because IPremiumEmailPolicy depends transitively on
            // scoped services (e.g. RemoteLicenseClient / DbContext on Self-Hosted).
            services.AddKeyedScoped<IEmailTransport, NoOpEmailTransport>(LicenseGatedEmailTransport.NoOpKey);
            services.AddScoped<IEmailTransport, LicenseGatedEmailTransport>();
        }
        else
        {
            // NoOp / unset / unknown → wire NoOp directly, no gate.
            services.AddScoped<IEmailTransport, NoOpEmailTransport>();
        }

        services.AddScoped<ISnapCdEmailSender, SnapCdEmailSender>();
        // Identity's IEmailSender<User> is satisfied by the same instance as the wrapper,
        // since ISnapCdEmailSender extends IEmailSender<User>.
        services.AddScoped<IEmailSender<User>>(sp => sp.GetRequiredService<ISnapCdEmailSender>());

        return services;
    }

    private static bool TryRegisterPremiumTransport(IServiceCollection services, ConfigurationManager configuration, string? provider)
    {
        switch (provider)
        {
            case "AmazonSES":
                services.AddOptions<AmazonSesEmailTransportSettings>()
                    .Bind(configuration.GetSection("EmailSender:AmazonSES"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddKeyedScoped<IEmailTransport, AmazonSesEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "SendGrid":
                services.AddOptions<SendGridEmailTransportSettings>()
                    .Bind(configuration.GetSection("EmailSender:SendGrid"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddKeyedScoped<IEmailTransport, SendGridEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Mailgun":
                services.AddOptions<MailgunEmailTransportSettings>()
                    .Bind(configuration.GetSection("EmailSender:Mailgun"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddKeyedScoped<IEmailTransport, MailgunEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Postmark":
                services.AddOptions<PostmarkEmailTransportSettings>()
                    .Bind(configuration.GetSection("EmailSender:Postmark"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddKeyedScoped<IEmailTransport, PostmarkEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Smtp":
                services.AddOptions<SmtpEmailTransportSettings>()
                    .Bind(configuration.GetSection("EmailSender:Smtp"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddKeyedScoped<IEmailTransport, SmtpEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            default:
                return false;
        }
    }
}
