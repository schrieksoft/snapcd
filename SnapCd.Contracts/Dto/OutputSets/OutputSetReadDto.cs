using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.OutputSets;

/// <summary>
/// DTO for OutputSet responses (GET operations).
/// </summary>
public class OutputSetReadDto : IDto
{
    public Guid Id { get; set; }
    
    public Guid ModuleId { get; set; }

    public long Timestamp { get; set; }

    public string Checksum { get; set; } = null!;
    public List<OutputReadDto>? Outputs { get; set; }
}
