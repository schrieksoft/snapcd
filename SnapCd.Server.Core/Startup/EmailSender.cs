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
            // scoped services (e.g. SaaSLicenseClient / DbContext on Self-Hosted).
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
                services.Configure<AmazonSesEmailTransportSettings>(configuration.GetSection("EmailSender:AmazonSES"));
                services.AddKeyedScoped<IEmailTransport, AmazonSesEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "SendGrid":
                services.Configure<SendGridEmailTransportSettings>(configuration.GetSection("EmailSender:SendGrid"));
                services.AddKeyedScoped<IEmailTransport, SendGridEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Mailgun":
                services.Configure<MailgunEmailTransportSettings>(configuration.GetSection("EmailSender:Mailgun"));
                services.AddKeyedScoped<IEmailTransport, MailgunEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Postmark":
                services.Configure<PostmarkEmailTransportSettings>(configuration.GetSection("EmailSender:Postmark"));
                services.AddKeyedScoped<IEmailTransport, PostmarkEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            case "Smtp":
                services.Configure<SmtpEmailTransportSettings>(configuration.GetSection("EmailSender:Smtp"));
                services.AddKeyedScoped<IEmailTransport, SmtpEmailTransport>(LicenseGatedEmailTransport.ConfiguredKey);
                return true;
            default:
                return false;
        }
    }
}
