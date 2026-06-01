// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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