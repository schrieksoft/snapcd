using SnapCd.Contracts.Dto.Variables;

namespace SnapCd.Contracts.Dto.VariableSets;

/// <summary>
/// DTO for creating a new VariableSet (POST operations).
/// </summary>
public class VariableSetCreateDto
{
    public Guid ModuleId { get; set; }
    public long Timestamp { get; set; }

    public string Checksum { get; set; } = null!;
    public List<VariableCreateDto>? Variables { get; set; }
}
