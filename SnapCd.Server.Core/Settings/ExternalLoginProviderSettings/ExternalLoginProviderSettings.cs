// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.ExternalLoginProviderSettings;

/// <summary>
/// SSO sign-in providers an operator can enable on the Dashboard's login page. Paid feature —
/// gated by the active license tier; on tiers that don't include SSO, the entire block is
/// zeroed out before OpenIddict registers anything (so configuring providers here on a
/// Community-tier deployment is a no-op).
/// </summary>
public class ExternalLoginProviderSettings
{
    /// <summary>Microsoft (Entra ID / Azure AD) login provider.</summary>
    public MicrosoftExternalLoginProvider Microsoft { get; set; } = new();

    /// <summary>Okta login provider.</summary>
    public OktaExternalLoginProvider Okta { get; set; } = new();

    /// <summary>Auth0 login provider.</summary>
    public Auth0ExternalLoginProvider Auth0 { get; set; } = new();

    /// <summary>Google login provider.</summary>
    public ExternalLoginProvider Google { get; set; } = new();

    /// <summary>GitHub login provider.</summary>
    public ExternalLoginProvider GitHub { get; set; } = new();
}

/// <summary>
/// Microsoft Entra ID (Azure AD) login provider configuration. Adds <see cref="TenantId"/> on
/// top of the common provider fields.
/// </summary>
public class MicrosoftExternalLoginProvider : ExternalLoginProvider
{
    /// <summary>
    /// Azure AD tenant ID the registered application lives in. Use <c>common</c> for multi-tenant
    /// apps, or a specific tenant GUID for single-tenant apps. Required when
    /// <see cref="ExternalLoginProvider.Enabled"/> is true.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Okta login provider configuration. Adds <see cref="Issuer"/> on top of the common provider fields.
/// </summary>
public class OktaExternalLoginProvider : ExternalLoginProvider
{
    /// <summary>
    /// Okta authorization-server issuer URL, typically
    /// <c>https://{your-okta-domain}/oauth2/default</c>. Required when
    /// <see cref="ExternalLoginProvider.Enabled"/> is true.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
}

/// <summary>
/// Auth0 login provider configuration. Adds <see cref="Issuer"/> on top of the common provider fields.
/// </summary>
public class Auth0ExternalLoginProvider : ExternalLoginProvider
{
    /// <summary>
    /// Auth0 tenant issuer URL, typically <c>https://{your-tenant}.auth0.com/</c>. Required when
    /// <see cref="ExternalLoginProvider.Enabled"/> is true.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
}

/// <summary>
/// Fields common to every supported external login provider. Specific providers may inherit and
/// add provider-specific fields (e.g. <see cref="MicrosoftExternalLoginProvider.TenantId"/>).
/// </summary>
public class ExternalLoginProvider
{
    /// <summary>
    /// When true, surface this provider as a sign-in option on the Dashboard. Defaults to false —
    /// providers must be opted-in explicitly per deployment.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>OAuth2 client ID issued by the provider's developer portal.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client secret paired with <see cref="ClientId"/>. Sensitive — source via the
    /// External Settings provider in production.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI registered with the provider. Must match the URI configured at the provider's
    /// end exactly; typically <c>{Server.Host}/signin-{provider}</c>.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;
}