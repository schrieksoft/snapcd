namespace SnapCd.Contracts.Dto.Outputs;

/// <summary>
/// DTO for creating a new Output (POST operations).
/// </summary>
public class OutputCreateDto
{
    public string Name { get; set; } = null!;
    public bool? Sensitive { get; set; }
    public string Type { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool FromExtraFile { get; set; } = false;
}
