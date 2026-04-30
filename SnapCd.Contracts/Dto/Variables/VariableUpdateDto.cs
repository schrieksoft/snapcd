using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Variables;

/// <summary>
/// DTO for updating an existing Variable (PUT operations).
/// </summary>
public class VariableUpdateDto : VariableCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
