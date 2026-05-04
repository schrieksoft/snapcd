namespace SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

public class ServicePrincipalToSeed
{
    public Guid? Id { get; set; }
    public required string ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public required string ClientType { get; set; }
    public required string? ConsentType { get; set; }
    public required string? DisplayName { get; set; }
    public required string? LoginRedirectUri { get; set; }
    public required string? LogoutRedirectUri { get; set; }
    public bool IsServicePrincipal { get; set; } = true;
    public List<string> Scopes { get; set; } = new();
    public Guid OrganizationId { get; set; } = Guid.Empty; // Default to NULL organization for system apps
}