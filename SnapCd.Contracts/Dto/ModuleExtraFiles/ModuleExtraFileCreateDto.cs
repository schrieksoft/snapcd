namespace SnapCd.Contracts.Dto.ModuleExtraFiles;

/// <summary>
/// DTO for creating a new ModuleExtraFile (POST operations).
/// </summary>
public class ModuleExtraFileCreateDto
{
    public Guid ModuleId { get; set; }

    public string FileName { get; set; } = null!;

    public string Contents { get; set; } = null!;

    public bool? Overwrite { get; set; }
}
