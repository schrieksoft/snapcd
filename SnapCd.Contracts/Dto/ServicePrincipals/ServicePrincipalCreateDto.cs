namespace SnapCd.Contracts.Dto.ServicePrincipals;

/// <summary>
/// DTO for creating a new ServicePrincipal (POST operations).
/// </summary>
public class ServicePrincipalCreateDto
{
    public string ClientId { get; set; } = null!;

    public string? ClientSecret { get; set; }

    public bool IsDisabled { get; set; }

    public List<string>? Scopes { get; set; } = ["snapcd_scope"];
}
