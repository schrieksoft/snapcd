using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;

public class DestroyFromPlanRequestBase : EngineJobRequestBase
{
    public string? DestroyBeforeHook { get; set; }
    public string? DestroyAfterHook { get; set; }

    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}
