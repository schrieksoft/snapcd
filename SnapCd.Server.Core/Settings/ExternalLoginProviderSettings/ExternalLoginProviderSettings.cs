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