using SnapCd.Contracts.Dto.Outputs;

namespace SnapCd.Contracts.Dto.OutputSets;

/// <summary>
/// DTO for creating a new OutputSet (POST operations).
/// </summary>
public class OutputSetCreateDto
{
    public long Timestamp { get; set; }

    public string Checksum { get; set; } = null!;
    
    public List<OutputCreateDto>? Outputs { get; set; }
}
