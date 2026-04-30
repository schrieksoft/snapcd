using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to create a Terraform plan.
/// Plan now receives pre-resolved Terraform variables from the server.
/// </summary>
public class PlanRequestBase : EngineJobRequestBase
{
    public string? PlanBeforeHook { get; set; }
    public string? PlanAfterHook { get; set; }

    public Dictionary<string, string> ResolvedParameters { get; set; } = null!;

    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}
