using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to get Terraform outputs.
/// </summary>
public class OutputRequestBase : EngineJobRequestBase
{
    public string? OutputBeforeHook { get; set; }
    public string? OutputAfterHook { get; set; }
    public List<string>? ExtraFileNames { get; set; }

    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}
