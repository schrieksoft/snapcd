namespace SnapCd.Contracts.Dto.Variables;

/// <summary>
/// DTO for creating a new Variable (POST operations).
/// </summary>
public class VariableCreateDto
{
    public string Name { get; set; } = null!;
    public string? Type { get; set; }
    public string? Description { get; set; }
    public bool Sensitive { get; set; } = false;
    public bool Nullable { get; set; } = false;
    public bool FromExtraFile { get; set; } = false;
}
