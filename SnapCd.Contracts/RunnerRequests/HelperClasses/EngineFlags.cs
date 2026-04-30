namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Boolean flags controlling Terraform and module behavior.
/// </summary>
public class EngineFlags
{
    public bool AutoUpgradeEnabled { get; set; }
    public bool AutoReconfigureEnabled { get; set; }
    public bool AutoMigrateEnabled { get; set; }
}
