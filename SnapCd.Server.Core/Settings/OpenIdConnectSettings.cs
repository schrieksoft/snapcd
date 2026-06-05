// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Settings.ExternalLoginProviderSettings;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// All OpenIdConnect / OpenIddict-related runtime settings: the symmetric key used to encrypt
/// issued JWTs, the RSA keypair used to sign them, and the optional external SSO providers
/// (Microsoft / Google / Okta / Auth0 / GitHub) the Server proxies authentication through.
/// </summary>
public class OpenIdConnectSettings
{
    /// <summary>
    /// Symmetric encryption key used to encrypt the access tokens the Server issues. Required.
    /// </summary>
    public TokenEncryptionSettings TokenEncryption { get; set; } = new();

    /// <summary>
    /// RSA keypair used to sign the access tokens the Server issues. Required.
    /// </summary>
    public TokenSigningSettings TokenSigning { get; set; } = new();

    /// <summary>
    /// Optional external SSO providers. SSO providers are a paid feature and are honoured only
    /// when the active license tier permits it; on tiers that don't include SSO, this block is
    /// silently zeroed out before the providers are registered.
    /// </summary>
    public ExternalLoginProviderSettings.ExternalLoginProviderSettings ExternalLoginProviders { get; set; } = new();
}

/// <summary>
/// Symmetric-key material used to encrypt JWTs the Server issues.
/// </summary>
public class TokenEncryptionSettings
{
    /// <summary>
    /// PEM-armoured base64-encoded AES-256 symmetric key, surrounded by
    /// <c>-----BEGIN SYMMETRIC KEY-----</c> / <c>-----END SYMMETRIC KEY-----</c> markers.
    /// Sensitive — production deployments must supply this via the External Settings provider
    /// rather than checking it into <c>appsettings.json</c>. The placeholder shipped in the
    /// default appsettings.json is for dev only.
    /// </summary>
    public string SymmetricKey { get; set; } = string.Empty;
}

/// <summary>
/// RSA keypair used to sign JWTs the Server issues. The public key is published at the
/// <c>/.well-known/openid-configuration</c> endpoint; the private key never leaves the process.
/// </summary>
public class TokenSigningSettings
{
    /// <summary>
    /// PEM-armoured RSA private key. Sensitive — must be supplied via the External Settings
    /// provider in production. The placeholder shipped in the default appsettings.json is for
    /// dev only.
    /// </summary>
    public string RsaPrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// PEM-armoured RSA public key matching <see cref="RsaPrivateKey"/>. Not sensitive, but must
    /// be the genuine pair to the private key or token validation will fail.
    /// </summary>
    public string RsaPublicKey { get; set; } = string.Empty;
}
