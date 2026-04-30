using SnapCd.Server.Core.Settings.ExternalLoginProviderSettings;

namespace SnapCd.Server.Core.Misc.Extensions;

public static class OpenIddictConfigurationExtensions
{
    public static OpenIddictClientWebIntegrationBuilder AddProviders(this OpenIddictClientWebIntegrationBuilder options,
        ExternalLoginProviderSettings settings)
    {
        if (settings.Microsoft.Enabled) AddMicrosoft(options, settings.Microsoft);

        if (settings.Okta.Enabled) AddOkta(options, settings.Okta);

        if (settings.Auth0.Enabled) AddAuth0(options, settings.Auth0);

        if (settings.Google.Enabled) AddGoogle(options, settings.Google);

        if (settings.GitHub.Enabled) AddGitHub(options, settings.GitHub);

        return options;
    }

    private static void AddMicrosoft(OpenIddictClientWebIntegrationBuilder options,
        MicrosoftExternalLoginProvider settings)
    {
        options.AddMicrosoft(providerOptions =>
        {
            providerOptions.SetClientId(settings.ClientId);
            providerOptions.SetClientSecret(settings.ClientSecret);
            providerOptions.SetRedirectUri(settings.RedirectUri);
            providerOptions.SetTenant(settings.TenantId); // Special Microsoft setting
            providerOptions.AddScopes("openid", "profile", "email");
        });
    }

    private static void AddOkta(OpenIddictClientWebIntegrationBuilder options, OktaExternalLoginProvider settings)
    {
        options.AddOkta(providerOptions =>
        {
            providerOptions.SetClientId(settings.ClientId);
            providerOptions.SetClientSecret(settings.ClientSecret);
            providerOptions.SetRedirectUri(settings.RedirectUri);
            providerOptions.SetIssuer(settings.Issuer);
            providerOptions.AddScopes("openid", "profile", "email");
        });
    }

    private static void AddAuth0(OpenIddictClientWebIntegrationBuilder options, Auth0ExternalLoginProvider settings)
    {
        options.AddAuth0(providerOptions =>
        {
            providerOptions.SetClientId(settings.ClientId);
            providerOptions.SetClientSecret(settings.ClientSecret);
            providerOptions.SetRedirectUri(settings.RedirectUri);
            providerOptions.SetIssuer(settings.Issuer);
            providerOptions.AddScopes("openid", "profile", "email");
        });
    }

    private static void AddGoogle(OpenIddictClientWebIntegrationBuilder options, ExternalLoginProvider settings)
    {
        options.AddGoogle(providerOptions =>
        {
            providerOptions.SetClientId(settings.ClientId);
            providerOptions.SetClientSecret(settings.ClientSecret);
            providerOptions.SetRedirectUri(settings.RedirectUri);
            providerOptions.AddScopes("openid", "profile", "email");
        });
    }

    private static void AddGitHub(OpenIddictClientWebIntegrationBuilder options, ExternalLoginProvider settings)
    {
        options.AddGitHub(providerOptions =>
        {
            providerOptions.SetClientId(settings.ClientId);
            providerOptions.SetClientSecret(settings.ClientSecret);
            providerOptions.SetRedirectUri(settings.RedirectUri);
            providerOptions.AddScopes("user:email");
        });
    }
}