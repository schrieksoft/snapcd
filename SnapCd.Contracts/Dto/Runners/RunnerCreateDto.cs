namespace SnapCd.Contracts.Dto.Runners;

/// <summary>
/// DTO for creating a new Runner (POST operations).
/// </summary>
public class RunnerCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDisabled { get; set; }

    public bool AllowMultipleInstances { get; set; }

    public bool IsAssignedToAllModules { get; set; } = false;
}
