using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Outputs;

/// <summary>
/// DTO for updating an existing Output (PUT operations).
/// </summary>
public class OutputUpdateDto : OutputCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
