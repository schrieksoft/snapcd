namespace SnapCd.Contracts.Dto.Groups;

/// <summary>
/// DTO for creating a new Group (POST operations).
/// </summary>
public class GroupCreateDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
