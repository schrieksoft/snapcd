using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

public static class TurnstileServices
{
    public static IServiceCollection AddTurnstileServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TurnstileSettings>(configuration.GetSection(TurnstileSettings.SectionName));

        services.AddHttpClient<TurnstileVerificationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
