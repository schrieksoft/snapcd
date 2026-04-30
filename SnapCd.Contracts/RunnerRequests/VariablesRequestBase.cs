using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to collect input variables.
/// Uses environment variables loaded from file (created by Init).
/// </summary>
public class VariablesRequestBase : EngineJobRequestBase
{
    public List<string>? ExtraFileNames { get; set; }
}
