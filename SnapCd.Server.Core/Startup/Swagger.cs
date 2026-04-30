using Microsoft.OpenApi.Models;

namespace SnapCd.Server.Core.Startup;

public static class Swagger
{
    public static IServiceCollection AddSnapCdSwaggerConfiguration(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSwaggerGen(c =>
        {
            //c.ResolveConflictingActions (apiDescriptions => apiDescriptions.First ());
            var serverHost = configuration["Server:Host"];
            var authorizationUrl = $"{serverHost}/connect/authorize";
            var tokenUrl = $"{serverHost}/connect/token";
            var scope = "snapcd_scope";

            c.AddSecurityDefinition("OpenIddict", new OpenApiSecurityScheme
            {
                Name = "OpenIddict",
                Description = "Authenticate with OpenIddict",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(authorizationUrl),
                        TokenUrl = new Uri(tokenUrl),
                        Scopes = new Dictionary<string, string>
                        {
                            { scope, "Access API as User" }
                        }
                    }
                }
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "OpenIddict", Type = ReferenceType.SecurityScheme
                        }
                    },
                    new List<string> { scope }
                }
            });
        });


        return services;
    }
}