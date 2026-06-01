// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.ExternalLoginProviderSettings;

public class ExternalLoginProviderSettings
{
    public MicrosoftExternalLoginProvider Microsoft { get; set; } = new();
    public OktaExternalLoginProvider Okta { get; set; } = new();
    public Auth0ExternalLoginProvider Auth0 { get; set; } = new();
    public ExternalLoginProvider Google { get; set; } = new();
    public ExternalLoginProvider GitHub { get; set; } = new();
}

public class MicrosoftExternalLoginProvider : ExternalLoginProvider
{
    public string TenantId { get; set; } = string.Empty;
}

public class OktaExternalLoginProvider : ExternalLoginProvider
{
    public string Issuer { get; set; } = string.Empty;
}

public class Auth0ExternalLoginProvider : ExternalLoginProvider
{
    public string Issuer { get; set; } = string.Empty;
}

public class ExternalLoginProvider
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}