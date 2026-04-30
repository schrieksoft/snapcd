using SnapCd.Contracts.Dto.Variables;
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.VariableSets;

/// <summary>
/// DTO for updating an existing VariableSet (PUT operations).
/// </summary>
public class VariableSetUpdateDto : IUpdateDto
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }

    public long Timestamp { get; set; }

    public string Checksum { get; set; } = null!;

    public List<VariableUpdateDto>? Variables { get; set; }
}
