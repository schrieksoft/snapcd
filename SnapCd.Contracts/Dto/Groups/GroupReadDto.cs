using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Groups;

/// <summary>
/// DTO for Group responses (GET operations).
/// </summary>
public class GroupReadDto : GroupCreateDto, IDto
{
    public Guid Id { get; set; }
}
