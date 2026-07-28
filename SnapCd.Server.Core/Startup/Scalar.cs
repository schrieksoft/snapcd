// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.OpenApi;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// OpenAPI document generation for the Scalar reference (/ApiReference). The built-in
/// ASP.NET Core generator emits OpenAPI 3.1 at /openapi/v1.json; Scalar is the only
/// renderer.
/// </summary>
public static class Scalar
{
    public static IServiceCollection AddSnapCdScalarConfiguration(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var serverHost = configuration["Server:Host"];
                var scope = "snapcd_scope";

                document.Info.Title = "Snap CD API";
                document.Info.Version = context.ApplicationServices
                    .GetRequiredService<IVersionService>().ShortVersion;

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["snapcd"] = new OpenApiSecurityScheme
                {
                    Name = "snapcd",
                    Description = "Authenticate with Snap CD User",
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{serverHost}/connect/authorize"),
                            TokenUrl = new Uri($"{serverHost}/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { scope, "Access API as User" }
                            }
                        }
                    }
                };

                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("snapcd", document)] = new List<string> { scope }
                });

                return Task.CompletedTask;
            });

            options.AddOperationTransformer<CurrentOrganizationOperationTransformer>();
        });

        return services;
    }
}
