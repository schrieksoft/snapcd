using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

namespace SnapCd.Server.Core.Settings.DataSeeder;

public class DebugDataSeederSettings
{
    public List<ServicePrincipalToSeed> ServicePrincipals { get; set; } = new();

    public List<UserToSeed> Users { get; set; } = new();

    public List<Stack> Stacks { get; set; } = new();

    public List<Runner> Runners { get; set; } = new();

    /// <summary>
    /// When true, seeds a debug-signed Enterprise license token onto the preseeded organization
    /// so debug runs can skip the paste-opaque-key flow. Defaults to false.
    /// </summary>
    public bool SeedLicenseToken { get; set; } = false;
}