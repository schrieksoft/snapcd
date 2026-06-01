// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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