// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Extensions;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Services.IdentityAccess;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.ExternalLoginProviderSettings;

namespace SnapCd.Server.Core.Startup;

public static class Auth
{
    public const string BearerAuthLogCategory = "SnapCd.Authentication.Bearer";

    public static IServiceCollection AddSnapCdAuthConfiguration(this IServiceCollection services, ConfigurationManager configuration, bool allowHttp)
    {
        var openIdConnectSettings = configuration.GetSection("OpenIdConnect").Get<OpenIdConnectSettings>() ?? new OpenIdConnectSettings();
        var externalLoginProviderSettings = openIdConnectSettings.ExternalLoginProviders;

        // EE: only register SSO providers if the organization has a valid EE license
        var ssoEnabled = SsoGatingService.ShouldEnableSsoAsync(services.BuildServiceProvider()).GetAwaiter().GetResult();
        if (!ssoEnabled)
        {
            externalLoginProviderSettings = new ExternalLoginProviderSettings();
        }

        var enableAuthorizationFlowOnClient = externalLoginProviderSettings.Microsoft.Enabled ||
                                              externalLoginProviderSettings.Okta.Enabled ||
                                              externalLoginProviderSettings.Auth0.Enabled ||
                                              externalLoginProviderSettings.Google.Enabled ||
                                              externalLoginProviderSettings.GitHub.Enabled;

        if (string.IsNullOrEmpty(openIdConnectSettings.TokenEncryption.SymmetricKey))
            throw new Exception("No value found for setting 'OpenIdConnect:TokenEncryption:SymmetricKey'. Snap CD Server requires a valid symmetric key here.");

        var symmetricKeyString = openIdConnectSettings.TokenEncryption.SymmetricKey;

        services.Configure<SecurityKeySettings>(c =>
        {
            var rsaPrivateKey = RSA.Create();
            rsaPrivateKey.ImportFromPem(openIdConnectSettings.TokenSigning.RsaPrivateKey);

            var rsaPublicKey = RSA.Create();
            rsaPublicKey.ImportFromPem(openIdConnectSettings.TokenSigning.RsaPublicKey);

            var symmetricKey = Convert.FromBase64String(SymmetricKeyUtils.ExtractBase64FromPem(symmetricKeyString));

            c.SymmetricEncryptionKey = new SymmetricSecurityKey(symmetricKey);
            c.RsaSigningPrivateKey = new RsaSecurityKey(rsaPrivateKey);
            c.RsaSigningPublicKey = new RsaSecurityKey(rsaPublicKey);
        });

        // see https://stackoverflow.com/questions/60957438/how-to-disable-the-default-authentication-scheme-when-non-default-schema-is-pro
        services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                }
            )
            .AddJwtBearer("Bearer", options =>
            {
                var securityKeySettings = services.BuildServiceProvider()
                    .GetRequiredService<IOptions<SecurityKeySettings>>()
                    .Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = $"{configuration["Server:Host"]}/",
                    ValidAudience = "snapcd",
                    IssuerSigningKey = securityKeySettings.RsaSigningPublicKey,
                    TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(SymmetricKeyUtils.ExtractBase64FromPem(symmetricKeyString)))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Support SignalR authentication via query string
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        var isHubPath = path.StartsWithSegments("/runnerhub") || path.StartsWithSegments("/agenthub");
                        if (!string.IsNullOrEmpty(accessToken) && isHubPath)
                            context.Token = accessToken;

                        if (isHubPath)
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>().CreateLogger(BearerAuthLogCategory);
                            logger.LogDebug(
                                "Hub bearer auth on {Path}: authHeader={HasAuthHeader} queryToken={HasQueryToken}",
                                path,
                                context.Request.Headers.ContainsKey("Authorization"),
                                !string.IsNullOrEmpty(accessToken));
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger(BearerAuthLogCategory);
                        logger.LogWarning(context.Exception,
                            "Bearer authentication failed for {Path}", context.HttpContext.Request.Path);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>().CreateLogger(BearerAuthLogCategory);
                        logger.LogDebug(
                            "Bearer challenge for {Path}: error={Error} description={Description}",
                            context.HttpContext.Request.Path, context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<SnapCdDbContext>();

                        // Extract the token ID (jti claim)
                        var tokenIdClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == "oi_tkn_id")
                            ?.Value;

                        if (string.IsNullOrEmpty(tokenIdClaim))
                        {
                            context.Fail("Token does not contain a valid 'oi_tkn_id' claim.");
                            return;
                        }

                        // Parse tokenId as Guid
                        if (!Guid.TryParse(tokenIdClaim, out var tokenId))
                        {
                            context.Fail(
                                $"Invalid 'oi_tkn_id' claim format. Token ID stored in \"oi_tkn_id\" equals \"{tokenId}\"");
                            return;
                        }

                        // Query the database asynchronously
                        var tokenRegistration = await dbContext.Tokens.FirstOrDefaultAsync(x => x.Id == tokenId);

                        if (tokenRegistration == null)
                        {
                            context.Fail(
                                $"Token is not registered in the system. Token ID stored in \"oi_tkn_id\" equals \"{tokenId}\"");
                            return;
                        }

                        // Optional: Perform additional checks, e.g., token revocation
                        if (tokenRegistration.Status == "revoked")
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        // Optional: Perform additional checks, e.g., token revocation
                        if (tokenRegistration.Status != "valid")
                        {
                            context.Fail(
                                $"Token does not have status 'valid'. Status was {tokenRegistration.Status}. Token ID stored in \"oi_tkn_id\" equals \"{tokenId}\"");
                            return;
                        }


                        // Validation succeeded
                        context.Success();
                    }
                };
            })
            .AddIdentityCookies();

        services.AddAuthorization(options =>
        {
            // // Default policy using the IdentityCookies scheme
            // options.DefaultPolicy = new AuthorizationPolicyBuilder()
            //     .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
            //     .RequireAuthenticatedUser()
            //     .Build();

            // Additional policy using the Bearer scheme
            options.AddPolicy("BearerPolicy", policy =>
            {
                policy.AddAuthenticationSchemes("Bearer")
                    .RequireAuthenticatedUser();
            });
        });

        // TODO should enable this only if debugger is attached
        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddIdentityCore<User>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SnapCdDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Register custom claims principal factory to add organization claims to Identity cookies
        services.AddScoped<IUserClaimsPrincipalFactory<User>, CustomUserClaimsPrincipalFactory>();

        // Add OpenIddict
        services.AddOpenIddict()
            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.

                options.UseEntityFrameworkCore()
                    .ReplaceDefaultEntities<ServicePrincipal, Authorization, Scope, Token, Guid>()
                    .UseDbContext<SnapCdDbContext>();

                // Enable Quartz.NET integration.
                options.UseQuartz();
            })

            // Register the OpenIddict client components.
            .AddClient(options =>
            {
                // Note: this sample uses the code flow, but you can enable the other flows if necessary.

                if (enableAuthorizationFlowOnClient)
                    options.AllowAuthorizationCodeFlow();

                options.AllowRefreshTokenFlow();

                // NOTE, technically these two keys do not need to be the same as the TokenEncryption/TokenSigning keys, but
                // for simplification we reuse those. These have to do with the comms between server and browser.

                var securityKeySettings = services.BuildServiceProvider()
                    .GetRequiredService<IOptions<SecurityKeySettings>>()
                    .Value;

                options.AddSigningKey(securityKeySettings.RsaSigningPrivateKey);
                options.AddEncryptionKey(securityKeySettings.SymmetricEncryptionKey);

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                    .EnableStatusCodePagesIntegration()
                    .EnableRedirectionEndpointPassthrough();

                // Register the System.Net.Http integration and use the identity of the current
                // assembly as a more specific user agent, which can be useful when dealing with
                // providers that use the user agent as a way to throttle requests (e.g Reddit).
                options.UseSystemNetHttp();
                //.SetProductInformation(typeof(Startup).Assembly); //TODO to enable this we'll likely have to move everything into a "Startup" class

                // Register the Web providers integrations.
                //
                // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
                // URI per provider, unless all the registered providers support returning a special "iss"
                // parameter containing their URL as part of authorization responses. For more information,
                // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
                options.UseWebProviders()
                    .AddProviders(externalLoginProviderSettings);
            })

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                if (allowHttp)
                    options.UseAspNetCore().DisableTransportSecurityRequirement();

                // Enable the authorization, logout, token and userinfo endpoints.
                options.SetAuthorizationEndpointUris("connect/authorize")
                    .SetEndSessionEndpointUris("connect/logout")
                    .SetIntrospectionEndpointUris("connect/introspect")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo");

                options.SetIssuer($"{configuration["Server:Host"]}/");

                // Mark the "email", "profile" and "roles" scopes as supported scopes.
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles);
                options.RegisterClaims(OpenIddictConstants.Claims.Role);
                options.RegisterClaims(OpenIddictConstants.Claims.ClientId);
                options.RegisterClaims(OpenIddictConstants.Claims.Issuer);

                // Register audiences and resources (required in OpenIddict 7.0)
                options.RegisterAudiences("snapcd");

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                var securityKeySettings = services.BuildServiceProvider()
                    .GetRequiredService<IOptions<SecurityKeySettings>>()
                    .Value;

                options.AddSigningKey(securityKeySettings.RsaSigningPrivateKey);
                options.AddEncryptionKey(securityKeySettings.SymmetricEncryptionKey);

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();
            })

            // Register the OpenIddict validation components.
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });


        return services;
    }
}