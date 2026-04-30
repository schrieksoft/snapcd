namespace SnapCd.Server.Core.Dtos.Organizations;

/// <summary>
/// DTO for creating a new Organization (POST operations).
/// </summary>
public class OrganizationCreateDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
