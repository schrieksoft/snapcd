using Microsoft.AspNetCore.HttpOverrides;

namespace SnapCd.Server.Core.Startup;

public static class Cors
{
    public static IServiceCollection AddSnapCdCorsConfiguration(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAnyOriginCorsPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}