using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Stacks;

/// <summary>
/// DTO for updating an existing Stack (PUT operations).
/// Includes Id to identify which stack to update.
/// </summary>
public class StackUpdateDto : StackCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
