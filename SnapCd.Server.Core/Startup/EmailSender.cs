using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Email;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

public static class EmailSender
{
    public static IServiceCollection AddSnapCdEmailSender(this IServiceCollection services, ConfigurationManager configuration)
    {
        if (configuration["EmailSender:EmailProvider"] == "AmazonSES")
        {
            services.Configure<AmazonSesEmailSenderSettings>(
                configuration.GetSection("EmailSender:AmazonSES"));
            services.AddSingleton<IEmailSender<User>, AmazonSesEmailSender>();
            services.AddSingleton<IEmailSenderWrapper, AmazonSesEmailSenderWrapper>();
        }
        else
        {
            // Default to no-op sender for development/testing
            services.AddSingleton<IEmailSender<User>, NoOpEmailSender>();
            services.AddSingleton<IEmailSenderWrapper, IdentityNoOpEmailSender>();
        }

        return services;
    }
}