using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Hubs.Filters;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.RunnerConnectionValidator;

namespace SnapCd.Server.Core.Startup;

public static class RunnerHubExtensions
{
    public static IServiceCollection AddSnapCdRunnerHub(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            // Increase from default 32KB to handle large plan data with many resources
            options.MaximumReceiveMessageSize =  1024 * 1024; // 1MB
            options.StreamBufferCapacity = 20;
            options.EnableDetailedErrors = true; // Help diagnose connection issues
        });
        services.AddSingleton<IHubFilter, TokenValidationFilter>();
        services.AddScoped<RunnerConnectionValidator>();
        services.AddScoped<RunnerJobAuthorizationService>();


        return services;
    }
}

