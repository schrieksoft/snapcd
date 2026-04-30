namespace SnapCd.Server.Core.Dtos;

public class DestroyModuleGraphDto
{
    public Guid RootModuleId { get; set; }
    public string RootModuleName { get; set; } = null!;
    public List<DestroyModuleNodeDto> NodeStates { get; set; } = new();

    /// <summary>
    /// Total number of modules that will be destroyed
    /// </summary>
    public int TotalModuleCount => NodeStates.Count;

    /// <summary>
    /// Number of stages in the destruction process
    /// </summary>
    public int TotalStages => NodeStates.Any() ? NodeStates.Max(n => n.Stage) : 0;
}