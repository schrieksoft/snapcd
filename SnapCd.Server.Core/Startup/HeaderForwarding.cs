using Microsoft.AspNetCore.HttpOverrides;

namespace SnapCd.Server.Core.Startup;

public static class HeaderForwarding
{
    public static IServiceCollection AddSnapCdHeaderForwardingConfiguration(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}