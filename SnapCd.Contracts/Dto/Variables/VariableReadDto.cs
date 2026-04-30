using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Variables;

/// <summary>
/// DTO for Variable responses (GET operations).
/// </summary>
public class VariableReadDto : VariableCreateDto, IDto
{
    public Guid Id { get; set; }
    
    public Guid VariableSetId  { get; set; }
}
