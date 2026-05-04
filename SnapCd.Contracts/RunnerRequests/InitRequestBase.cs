using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;


public class InitRequestBase : EngineJobRequestBase
{
    public string? InitBeforeHook { get; set; }
    public string? InitAfterHook { get; set; }
    public EngineBackendConfiguration BackendConfiguration { get; set; } = null!;
    public bool CleanInitEnabled { get; set; }
    public Dictionary<string, string> ResolvedEnvVars { get; set; } = null!;
}
