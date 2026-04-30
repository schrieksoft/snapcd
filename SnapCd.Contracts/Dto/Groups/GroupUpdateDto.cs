using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Groups;

/// <summary>
/// DTO for updating an existing Group (PUT operations).
/// </summary>
public class GroupUpdateDto : GroupCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
