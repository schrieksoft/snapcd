using SnapCd.Server.Core.Licensing.Models;

namespace SnapCd.Server.Core.Licensing.Attributes;

/// <summary>
/// Marks a controller action as requiring a tier that includes the given <see cref="Feature"/>.
/// The action filter consults <see cref="TierFeatures"/> to make the gate decision.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class VerifyLicense(Feature feature) : Attribute
{
    public Feature Feature { get; } = feature;
}
